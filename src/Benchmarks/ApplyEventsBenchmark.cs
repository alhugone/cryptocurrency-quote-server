using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CoinbaseStreamRecording.SessionRecorder;
using QuoteServer.OrderBook.OrderBookComputing;
using QuoteServer.OrderBook.Partition.Events;

namespace Benchmarks
{
    [MemoryDiagnoser]
    public class ApplyEventsBenchmark
    {
        private static readonly ISessionRecorder SessionRecorded;

        static ApplyEventsBenchmark()
        {
            SessionRecorded = SessionRecorderFactory.GetStreamRecorder();
            SessionRecorded.ReloadWithLastRecordedSessionData();
            Events = SessionRecorded.FindEventsBetweenSequences(
                                        SessionRecorded.OrderBookL3Snapshots.First().Sequence,
                                        SessionRecorded.OrderBookL3Snapshots.Last().Sequence
                                    )
                                    .ToList();
        }

        private static List<OrderBookModifyiableEvent> Events { get; }

        [Benchmark]
        public void ConcatStringsUsingStringBuilder()
        {
            var orderBook = OrderBookFactory.From(
                2,
                SessionRecorded.OrderBookL3Snapshots[0].Asks,
                SessionRecorded.OrderBookL3Snapshots[0].Bids
            );
            foreach (var orderBookModifyiableEvent in Events) orderBook.Apply(orderBookModifyiableEvent);
        }
    }
}