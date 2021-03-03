# Quotes Service

QuoteService purpose is to propagate messages from one or more QuoteSource to subscribed clients as fast as possible - in a real-time like fashion.

QuoteSources are 3rd party services that we know nothing about internal architecture, except it streams messages that we subscribed to. The assumption is that all trading events are broadcast equally and there is no reason for creating two separate connections for the same pair inside a single QuoteService instance. In theory, under some circumstances, it could be worth but it would require to design a solution tailored to the internals of 3rd party service, and we want to be agnostic of it.

Below is a view of QuoteService architecture design: 

![Quote Server Architecture][QuoteServerArchitecture]


## Quotes Partition

Quotes partition is a unit of scaling, both vertical and horizontal. The single partition maintains:

- a single exclusive connection to a single source.
- a configurable set of trading pairs
- a configurable set of Reader's threads

Partitions give the possibility of configuration inside a single application server instance. can be set up in many ways:
- 1..* per app instance
- 1..* per machine

Each partition is stateless, not aware of any other.

Specific set up should depend on needs, vertical/horizontal scaling. for example, two partitions should maintain exclusive sets, except for horizontal scaling. They allow using resources like:

- assing partition connection per NIC
- configure partitions for NUMA architecture to maintains it's data in assigned memory

Below are sample configurations:
 
![a][quotes_partitions_arrangement]

### Single connection

Partition handles only a single connection. The assumption is that its bandwidth is used at a very high level so another would bring dropping messages. In the case of multiple NICs, there should be one Partition per Nic with an exclusive set of trading pairs. 

There is nothing that would not allow maintaining multiple connections to the same or different source, but right now the assumption is that the single connection is used at 100%.

### Queues

Each Queue is only for singe trading pair. It guarantees thread safety and orders of messages.
The queue could maintain also trading pairs from different sources or be unique per (Source, TradingPair).
The queue could be a 2-level queue
- 1st level fixed-length ring-based fast lock-free array based
- 2nd level regular synchronized thread

2nd level should be used only when the 1st level is full.
The size of the 1st level should be chosen that way it should be an extremely rare situation to switch to the 2nd level.


### Broker

The broker is a single thread that receives messages from the connection and put them in the proper message queue. 
Putting the message is non-blocking. 
There is no point in maintaining more than one thread per connection as it would drive out of order messages due to race conditions. Synchronization, sorting etc. could bring more problems and overhead, it would be worth only when knowing something more about the distribution of trading pair messages.
Broker thread can run on a higher priority than the rest of threads, as the assumption is that it will never wait for messages as a stream is extremely fast.

### Readers

A single reader is a single thread that reads from one or more queues. Queues are not shared between readers due to the possibility of delivering messages out of order.
The number of readers should be chosen both to hardware architecture and CPU capabilities and subscribed pairs. There is no benefit of creating more readers than N-1.
Its better to distribute subscribed pairs based on statistic - how much events trading pair generate. To have it fairy distributed between readers.
It could be a dynamic that readers switch their queues, but it would require to build some coordinator, so right now it is out of scope.

Also on Reader's thread, there will be more work executed than just reading as by default it will be 'part' of 'handling' message. Handling message can be distributed to other ThreadsPool easy but it depends on usage.

#### Batching messages

By default, the reader gets one message from the queue at a time. But batching messages and passing them to subscribers brings much better results. That's why the reader tries to read more messages if there are exists in the queue and pass them in a single event as it will bring much of performance - benefit.

For example, it can read first and then try to read N next messages in a non-blocking way:

```
If the queue has a message then
    take message
    while queue has more messages take N next messages from it non-blocking
    propagate messages
else go to next queue
```

It can be base on N next messages or some quant of time when the high-frequency clock is available.


### Thread prioritising

A sensible solution is to have priorities as follow:

```
Broker's thread priority >= Readers' threads priority >= Default thread priority
```

Of course dependent on statistics and real configuration there would be a point in making Readers highers than BrokerThread. For example when they would have to do more work.


### Garbage-free

Partition should not emmit objects idealy it should reuse objects, use structs or other mechanism. 

[QuoteServerArchitecture]: ./docs/assets/quotes_server.png 
[quotes_partitions_arrangement]: ./docs/assets/quotes_partitions_arrangement.png # cryptocurrency-quote-server
# cryptocurrency-quote-server
