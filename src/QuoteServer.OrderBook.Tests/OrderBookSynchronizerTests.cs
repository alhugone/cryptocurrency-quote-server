using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CoinbaseStreamRecording.SessionRecorder;
using FluentAssertions;
using QuoteServer.OrderBook.OrderBookComputing;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;
using Xunit;

namespace QuoteServer.OrderBook.Tests
{
    public class OrderBookSynchronizerTests
    {
        [Fact]
        public void Apply_UpdatesOrderBookState_FromOneL3SnapshotToNext()
        {
            var sessionRecorder = SessionRecorderFactory.GetStreamRecorder();
            sessionRecorder.ReloadWithLastRecordedSessionData();
            for (var i = 0; i < sessionRecorder.OrderBookL3Snapshots.Count - 1; i++)
                try
                {
                    var snapshot1 = sessionRecorder.OrderBookL3Snapshots[i];
                    var expectedSnapshot = sessionRecorder.OrderBookL3Snapshots[i + 1];
                    var eventsStream = sessionRecorder.Events.TakeWhile(x => x.Sequence <= expectedSnapshot.Sequence)
                                                      .ToObservable();
                    var orderBookSynchronizer = new OrderBookSynchronizer(OrderBookFactory.Empty(), eventsStream);
                    orderBookSynchronizer.SynchronizeOrderBookInitialStateWith(snapshot1);
                    // act
                    var orderBook = orderBookSynchronizer.TakeOrderBookCopy();
                    var snapshot = orderBookSynchronizer.TakeOrderBookL3Snapshot();
                    // assert
                    AssertBothOrderBooksAreSame(orderBook, OrderBookFactory.From(expectedSnapshot));
                    AssertSnapshotHasCorrectOrder(snapshot, expectedSnapshot);
                }
                catch (Exception ex)
                {
                    throw new Exception($"I: {i} of {sessionRecorder.OrderBookL3Snapshots.Count}", ex);
                }
        }

        [Fact]
        public void Apply_UpdatesOrderBookState_SoThatTakeOrderBookL2Snapshot_ReturnsExpectedSnapshot()
        {
            var sessionRecorder = SessionRecorderFactory.GetStreamRecorder();
            sessionRecorder.ReloadWithLastRecordedSessionData();
            for (var i = 0; i < sessionRecorder.OrderBookL2Snapshots.Count; i++)
                try
                {
                    var expectedSnapshot = sessionRecorder.OrderBookL2Snapshots[i];
                    var initStateSnapshotL3 =
                        sessionRecorder.OrderBookL3Snapshots.First(x => x.Sequence <= expectedSnapshot.Sequence);
                    // var expectedSnapshot = sessionRecorder.OrderBookL2Snapshots[i + 1];
                    var eventsStream = sessionRecorder.Events.TakeWhile(x => x.Sequence <= expectedSnapshot.Sequence)
                                                      .ToObservable();
                    var orderBookSynchronizer = new OrderBookSynchronizer(OrderBookFactory.Empty(), eventsStream);
                    orderBookSynchronizer.SynchronizeOrderBookInitialStateWith(initStateSnapshotL3);
                    // act
                    var snapshot = orderBookSynchronizer.TakeOrderBookL2Snapshot().TrimToTop50();
                    // assert
                    AssertSnapshotHasCorrectOrder(snapshot, expectedSnapshot);
                }
                catch (Exception ex)
                {
                    throw new Exception($"I: {i} of {sessionRecorder.OrderBookL2Snapshots.Count}", ex);
                }
        }

        private void AssertSnapshotHasCorrectOrder(OrderBookL2Snapshot snapshot, OrderBookL2Snapshot expectedSnapshot)
        {
            AssertSameOrder(snapshot.Asks, expectedSnapshot.Asks);
            AssertSameOrder(snapshot.Bids, expectedSnapshot.Bids);
        }

        private void AssertSameOrder(List<L2SnapshotQuote> snapshot, List<L2SnapshotQuote> expectedSnapshot)
        {
            snapshot.Should().HaveCount(expectedSnapshot.Count);
            for (var j = 0; j < snapshot.Count; j++)
            {
                snapshot[j].Price.Should().Be(expectedSnapshot[j].Price);
                snapshot[j].OrdersCount.Should().Be(expectedSnapshot[j].OrdersCount);
                snapshot[j].Size.Should().Be(expectedSnapshot[j].Size);
            }
        }

        private static void AssertSnapshotHasCorrectOrder(
            OrderBookL3Snapshot snapshot,
            OrderBookL3Snapshot expectedSnapshot)
        {
            AssertSameOrder(snapshot.Asks, expectedSnapshot.Asks);
            AssertSameOrder(snapshot.Bids, expectedSnapshot.Bids);
        }

        private static void AssertSameOrder(List<Quote> snapshot, List<Quote> expectedSnapshot)
        {
            snapshot.Should().HaveCount(expectedSnapshot.Count);
            for (var j = 0; j < snapshot.Count; j++)
            {
                expectedSnapshot[j].Price.Should().Be(snapshot[j].Price);
                expectedSnapshot[j].OrderId.Should().Be(snapshot[j].OrderId);
            }
        }

        private static void AssertBothOrderBooksAreSame(IOrderBook initialOrderBook, IOrderBook expectedOrderBook)
        {
            // assert
            var initialOrderBookAsks = initialOrderBook.Asks.ToDictionary(x => x.OrderId);
            var initialOrderBookBids = initialOrderBook.Bids.ToDictionary(x => x.OrderId);
            var expectedOrderBookAsks = expectedOrderBook.Asks.ToDictionary(x => x.OrderId);
            var expectedOrderBookBids = expectedOrderBook.Bids.ToDictionary(x => x.OrderId);
            initialOrderBook.Sequence.Should().Be(expectedOrderBook.Sequence);
            initialOrderBookAsks.Count.Should().Be(expectedOrderBookAsks.Count);
            initialOrderBookBids.Count.Should().Be(expectedOrderBookBids.Count);
            ShouldBeSame(expectedOrderBookAsks, initialOrderBookAsks);
            ShouldBeSame(expectedOrderBookBids, initialOrderBookBids);
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