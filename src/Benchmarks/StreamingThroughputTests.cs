using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.Diagnostics.Tracing.Parsers;
using Moq;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Partition.Settings;
using QuoteServer.OrderBook.Primitives;

namespace Benchmarks
{
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [GcServer(true)]
    [SimpleJob(RunStrategy.Throughput, 0, 2, 5)]
    public class StreamingThroughputTests
    {
        [Benchmark]
        [Arguments(1000, 500, 1)]
        [Arguments(1000, 500, 2)]
        [Arguments(1000, 500, 3)]
        [Arguments(1000, 500, 4)]
        [Arguments(1000, 500, 5)]
        [Arguments(1000, 500, 6)]
        [Arguments(1000, 500, 7)]
        [Arguments(1000, 500, 8)]
        [Arguments(1000, 500, 9)]
        [Arguments(1000, 500, 10)]
        [Arguments(1000, 500, 11)]
        [Arguments(1000, 500, 12)]
        [Arguments(1000, 500, 14)]
        [Arguments(1000, 500, 20)]
        public void TestFor(int subscribers, int pairsToSubscribe, int readers = 1)
        {
            RunWhileNotProcessedExpectedEvents(subscribers, pairsToSubscribe, readers);
        }

        private void RunWhileNotProcessedExpectedEvents(
            int subscriptions,
            int pairs,
            int readers,
            long expectedProcessedEvents = 10_000_000)
        {
            var source = new QuotesPartition(
                Mock.Of<IQuotesSourceConnection>(),
                new QuotesPartitionSettings
                {
                    TradingPairs = new HashSet<TradingPair>(Enumerable.Range(0, 1001).Select(x => (TradingPair) x)),
                    NumberOfReaders = readers,
                }
            );
            var subscribers = new List<IDisposable>(subscriptions);
            var currentPair = 0;
            var subsCount = new int[subscriptions];
            for (var i = 0; i < subscriptions; i++)
            {
                var subnum = i;
                var subscription = source.Streams((TradingPair) (currentPair++ % pairs))
                                         .Subscribe(
                                             x =>
                                             {
                                                 subsCount[subnum]++;
                                                 var p = 0;
                                                 while (p++ < 100) "dkslfasjdkfljsadlfksdfljsdkf".GetHashCode();
                                             }
                                         );
                subscribers.Add(subscription);
            }
            var list = new List<Task>();
            var messages = new Opened[pairs];
            for (var i = 0; i < pairs; i++)
                messages[i] = new Opened {OrderId = Guid.NewGuid(), Time = DateTime.Now, TradingPair = (TradingPair) i};
            var stopwatch = Stopwatch.StartNew();
            const int max = 1;
            for (var i = 0; i < max; i++)
                list.Add(
                    Task.Run(
                        () =>
                        {
                            var curr1 = 0;
                            Thread.CurrentThread.Priority = ThreadPriority.Highest;
                            while (curr1 < expectedProcessedEvents / max)
                            {
                                source.OnOrderBookEventReceived(messages[curr1 % pairs]);
                                curr1++;
                            }
                            Thread.CurrentThread.Priority = ThreadPriority.Normal;
                        }
                    )
                );
            Task.WaitAll(list.ToArray());
            var whenFinishedBroadcast = stopwatch.Elapsed;
            while (subsCount.Sum() < expectedProcessedEvents) Thread.Yield();
            stopwatch.Stop();
            source.Dispose();
            Console.WriteLine(
                $@"=========================================
Emitting finished at:          {whenFinishedBroadcast}
Then waited to process events: {stopwatch.Elapsed - whenFinishedBroadcast} 
Total Time:                    {stopwatch.Elapsed}
"
            );
        }
    }
}