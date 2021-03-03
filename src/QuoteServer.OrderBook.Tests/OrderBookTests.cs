using System;
using System.Collections.Generic;
using System.Linq;
using CoinbaseStreamRecording.SessionRecorder;
using FluentAssertions;
using QuoteServer.OrderBook.OrderBookComputing;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook;
using Xunit;

namespace QuoteServer.OrderBook.Tests
{
    public class OrderBookTests
    {
        [Fact]
        public void Apply_UpdatesOrderBookState_FromOneSnapshotToNext()
        {
            var sessionRecorder = SessionRecorderFactory.GetStreamRecorder();
            sessionRecorder.ReloadWithLastRecordedSessionData();
            for (var i = 0; i < sessionRecorder.OrderBookL3Snapshots.Count - 1; i++)
                try
                {
                    var snapshot1 = sessionRecorder.OrderBookL3Snapshots[i];
                    var initialOrderBook = OrderBookFactory.From(snapshot1);
                    var expectedOrderBook = OrderBookFactory.From(sessionRecorder.OrderBookL3Snapshots[i + 1]);
                    var eventsBetweenSnapshots = sessionRecorder.FindEventsBetweenSnapshots(i, i + 1).ToArray();
                    ApplyingEventsOnInitialOrderBookRestoresExpectedOrderBook(
                        initialOrderBook,
                        expectedOrderBook,
                        eventsBetweenSnapshots
                    );
                }
                catch (Exception ex)
                {
                    throw new Exception($"I: {i} of {sessionRecorder.OrderBookL3Snapshots.Count}", ex);
                }
        }

        private static void ApplyingEventsOnInitialOrderBookRestoresExpectedOrderBook(
            IOrderBook initialOrderBook,
            IOrderBook expectedOrderBook,
            OrderBookModifyiableEvent[] eventsBetweenOrderBooks)
        {
            // act
            long lastSequence = 0;
            foreach (var @event in eventsBetweenOrderBooks)
            {
                if (lastSequence > @event.Sequence) throw new Exception("Out of Order");
                lastSequence = @event.Sequence;
                initialOrderBook.Apply(@event);
            }
            // assert
            var initialOrderBookAsks = initialOrderBook.Asks.ToDictionary(x => x.OrderId);
            var initialOrderBookBids = initialOrderBook.Bids.ToDictionary(x => x.OrderId);
            var expectedOrderBookAsks = expectedOrderBook.Asks.ToDictionary(x => x.OrderId);
            var expectedOrderBookBids = expectedOrderBook.Bids.ToDictionary(x => x.OrderId);
            initialOrderBookAsks.Count.Should().Be(expectedOrderBookAsks.Count);
            initialOrderBookBids.Count.Should().Be(expectedOrderBookBids.Count);
            ShouldBeSame(expectedOrderBookAsks, initialOrderBookAsks);
            ShouldBeSame(expectedOrderBookBids, initialOrderBookBids);
            initialOrderBook.Sequence.Should().Be(expectedOrderBook.Sequence);
        }

        private static void ShouldBeSame(
            Dictionary<Guid, Quote> expectedOrderBookAsks,
            Dictionary<Guid, Quote> initialOrderBookAsks)
        {
            foreach (var expectedAsk in expectedOrderBookAsks)
            {
                var ask = initialOrderBookAsks[expectedAsk.Key];
                ask.Size.Should().Be(expectedAsk.Value.Size);
                ask.Price.Should().Be(expectedAsk.Value.Price);
            }
        }
    }
}