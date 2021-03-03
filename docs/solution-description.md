
# Solution

## Communication 

- gRPC is used to Client-QuoteServer communication
- webSocket are used to communicate CoinbaseSource-Coinbase


```ascii
QuoteServer
Client                      gRpc Service          CoinbaseSource     Coinbase
|     Subscribe To Pair       +                        +                 +
+---------------------------->+                        |                 |
|                             |                        |   Push EVENTS   |
|                             |                        +<----------------+
|                             |  Get Stream For PAIR   |                 |
|                             +----------------------->+                 |
|                             |      Push EVENTS       |                 |
|     Push EVENTS             +<-----------------------+                 |
+<----------------------------+                        |                 |
|                             |                        |                 |
|                             |                        |                 |
+                             +                        +                 +

```

## Source

It's abstraction of QuotesSource, that is:

```
interface IQuotesSource : IDisposable
{
    Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(Pair pair);
    IObservable<OrderBookModifyiableEvent> Streams(Pair pair);
}
```

It exposes an interface for getting Snapshot and Subscribing to a stream of events for a given pair.
It assumes that managing connections, subscription, detecting unsubscription will be implemented by concrete implementation;

## CoinbaseSource (And each next)

It's a concrete implementation of QuotesSource that acts as an adapter, that is:

- Expose streams of events for pair
- manages under the hood everything that is needed to get data from Coinbase
  - track subscriptions on specific Pairs
  - subscribe and unsubscribe with CoinbaseAPI
  - pushes events from Coinbase to subscribed streams
  - detect when there are no subscribers on Pair and unsubscribe from Coinbase for that Pair

## OrderBooksController

Manages L3 OrderBooks snapshots of requested Pairs.
Creates just one snapshot for the Pair.

### Creating OrderBook

- subscribes to stream for given pair
- buffer events
- asks Coinbase for a snapshot
- uses return snapshot as initial for the OrderBook
- forwards each event to OrderBook
- when snapshot sequence is less than first buffered events, asks for a new until snapshot's sequence is greater than the sequence in the buffer

## Not handled

This solution assumes a continuous ordered stream of events. It does not handle order book in a correct way when some events are missing or are out of order.
When checking it I haven't such a situation for streams that were running ~4 hours. It doesn't mean it not occur. Just find out that it's not a problem for part of this task, to show sample implementation.
Detecting is quite easy, it could then reuse existing code to start from a new snapshot.