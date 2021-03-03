using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinbaseStreamRecording.SessionRecorder;
using FluentAssertions;
using QuoteServer.OrderBook.OrderBookComputing;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;
using Xunit;

namespace CoinbaseClient.Tests
{
    public class ConinBaseRecordingHelpers
    {
        [Fact]
        public async Task RecordStreamsAndSnapshot_WithValidation()
        {
            const TradingPair productType = TradingPair.BtcUsd;
            var qoutoeStream = CoinbaseQuoteSourceFactory.GetCoinbaseQuoteSource();
            var streamRecored = SessionRecorderFactory.GetStreamRecorder();
            qoutoeStream.Streams(productType).Subscribe(x => streamRecored.Record(x));
            // act
            await Task.Delay(5000); // fill with events before first OrderBookSnapshot
            var orderBookL3Snapshot = await qoutoeStream.GetOrderBookL3Snapshot(productType);
            var orderBookL2Snapshot = await qoutoeStream.GetOrderBookL2Snapshot(productType);
            if (orderBookL2Snapshot.Sequence < streamRecored.MinSequence ||
                orderBookL3Snapshot.Sequence < streamRecored.MinSequence)
                throw new Exception("Not this time, some events are missing since the first snapshot");
            streamRecored.Record(orderBookL3Snapshot);
            var random = new Random();
            var snapshotsTaken = 0;
            while (streamRecored.Statistics.TotalEvents < 4_000 || snapshotsTaken < 20)
            {
                Thread.Sleep(random.Next(350, 2000));
                orderBookL2Snapshot = await qoutoeStream.GetOrderBookL2Snapshot(productType);
                streamRecored.Record(orderBookL2Snapshot);
                orderBookL3Snapshot = await qoutoeStream.GetOrderBookL3Snapshot(productType);
                streamRecored.Record(orderBookL3Snapshot);
                snapshotsTaken++;
            }
            var sync = new AutoResetEvent(false);
            streamRecored.WhenSequence(
                Math.Max(orderBookL2Snapshot.Sequence, orderBookL3Snapshot.Sequence),
                () => { sync.Set(); }
            );
            sync.WaitOne(); //wait for rest of events up to second snapshot
            qoutoeStream.Dispose();
            var pathToPersistedSession = streamRecored.PersistSession();
            var streamRecorderLoad = SessionRecorderFactory.GetStreamRecorder();
            streamRecorderLoad.Load(pathToPersistedSession);
            // assert
            streamRecorderLoad.Should().BeEquivalentTo(streamRecored, x => x.Excluding(y => y.Statistics));
        }

        [Fact]
        public void ReloadWithLastRecordedSessionData()
        {
            var streamRecorderLoad = SessionRecorderFactory.GetStreamRecorder();
            streamRecorderLoad.ReloadWithLastRecordedSessionData();
        }

        public class LastRecoredSessionValidation
        {
            [Fact]
            public void Ask_ForEachNewOrder_OpenEvent_With_SellSide_Exists()
            {
                var streamRecorder = SessionRecorderFactory.GetStreamRecorder();
                streamRecorder.ReloadWithLastRecordedSessionData();
                for (var i = 0; i < streamRecorder.OrderBookL3Snapshots.Count - 1; i++)
                {
                    var firstSnapshot = streamRecorder.OrderBookL3Snapshots[i];
                    var secondSnapshot = streamRecorder.OrderBookL3Snapshots[i + 1];
                    var orderBook1 = OrderBookFactory.From(
                        firstSnapshot.Sequence,
                        firstSnapshot.Asks,
                        firstSnapshot.Bids
                    );
                    var orderBook2 = OrderBookFactory.From(
                        secondSnapshot.Sequence,
                        secondSnapshot.Asks,
                        secondSnapshot.Bids
                    );
                    firstSnapshot.Sequence.Should().BeLessThan(secondSnapshot.Sequence);
                    // act
                    var newOrderIds = orderBook2.Asks.Select(x => x.OrderId)
                                                .Distinct()
                                                .Except(orderBook1.Asks.Select(x => x.OrderId).Distinct())
                                                .ToHashSet();
                    var openEventsForNewOrderIds = streamRecorder.FindEventsBetweenSnapshots(i, i + 1)
                                                                 .OfType<Opened>()
                                                                 .Where(
                                                                     x => x.Side == OrderSide.Sell &&
                                                                          newOrderIds.Contains(x.OrderId)
                                                                 )
                                                                 .Select(x => x.OrderId)
                                                                 .Distinct();
                    newOrderIds.Count.Should().Be(openEventsForNewOrderIds.Count());
                }
            }

            [Fact]
            public void Ask_ForEachRemovedOrder_DoneEvent_With_SellSide_Exists()
            {
                var streamRecorder = SessionRecorderFactory.GetStreamRecorder();
                streamRecorder.ReloadWithLastRecordedSessionData();
                var firstResponse = streamRecorder.OrderBookL3Snapshots[0];
                var secondResponse = streamRecorder.OrderBookL3Snapshots[1];
                var orderBook1 = OrderBookFactory.From(firstResponse.Sequence, firstResponse.Asks, firstResponse.Bids);
                var orderBook2 = OrderBookFactory.From(
                    secondResponse.Sequence,
                    secondResponse.Asks,
                    secondResponse.Bids
                );
                // act
                var removedOrderIds = orderBook1.Asks.Select(x => x.OrderId)
                                                .Distinct()
                                                .Except(orderBook2.Asks.Select(x => x.OrderId).Distinct())
                                                .ToHashSet();
                var doneEventsForRemovedOrderIds = streamRecorder.Events.OfType<Closed>()
                                                                 .Where(
                                                                     x => x.Side == OrderSide.Sell &&
                                                                          removedOrderIds.Contains(x.OrderId)
                                                                 )
                                                                 .Select(x => x.OrderId)
                                                                 .Distinct();
                // assert
                removedOrderIds.Count.Should().Be(doneEventsForRemovedOrderIds.Count());
            }

            [Fact]
            public void Bid_ForEachNewOrder_OpenEvent_With_BuySide_Exists()
            {
                var streamRecorder = SessionRecorderFactory.GetStreamRecorder();
                streamRecorder.ReloadWithLastRecordedSessionData();
                var firstResponse = streamRecorder.OrderBookL3Snapshots[0];
                var secondResponse = streamRecorder.OrderBookL3Snapshots[1];
                var orderBook1 = OrderBookFactory.From(firstResponse.Sequence, firstResponse.Asks, firstResponse.Bids);
                var orderBook2 = OrderBookFactory.From(
                    secondResponse.Sequence,
                    secondResponse.Asks,
                    secondResponse.Bids
                );
                firstResponse.Sequence.Should().BeLessThan(secondResponse.Sequence);
                // act
                var newOrderIds = orderBook2.Bids.Select(x => x.OrderId)
                                            .Distinct()
                                            .Except(orderBook1.Bids.Select(x => x.OrderId).Distinct())
                                            .ToHashSet();
                var openEventsForNewOrderIds = streamRecorder.Events.OfType<Opened>()
                                                             .Where(
                                                                 x => x.Side == OrderSide.Buy &&
                                                                      newOrderIds.Contains(x.OrderId)
                                                             )
                                                             .Select(x => x.OrderId)
                                                             .Distinct();
                newOrderIds.Count.Should().Be(openEventsForNewOrderIds.Count());
            }

            [Fact]
            public void Bid_ForEachRemovedOrder_DoneEvent_With_BuySide_Exists()
            {
                var streamRecorder = SessionRecorderFactory.GetStreamRecorder();
                streamRecorder.ReloadWithLastRecordedSessionData();
                var firstResponse = streamRecorder.OrderBookL3Snapshots[0];
                var secondResponse = streamRecorder.OrderBookL3Snapshots[1];
                var orderBook1 = OrderBookFactory.From(firstResponse.Sequence, firstResponse.Asks, firstResponse.Bids);
                var orderBook2 = OrderBookFactory.From(
                    secondResponse.Sequence,
                    secondResponse.Asks,
                    secondResponse.Bids
                );
                // act
                var removedOrderIds = orderBook1.Bids.Select(x => x.OrderId)
                                                .Distinct()
                                                .Except(orderBook2.Bids.Select(x => x.OrderId).Distinct())
                                                .ToHashSet();
                var doneEventsForRemovedOrderIds = streamRecorder.Events.OfType<Closed>()
                                                                 .Where(
                                                                     x => x.Side == OrderSide.Buy &&
                                                                          removedOrderIds.Contains(x.OrderId)
                                                                 )
                                                                 .Select(x => x.OrderId)
                                                                 .Distinct();
                // assert
                removedOrderIds.Count.Should().Be(doneEventsForRemovedOrderIds.Count());
            }
        }
    }
}