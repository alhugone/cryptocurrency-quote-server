using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;
using Xunit;

namespace CoinbaseClient.Tests
{
    public class CoinbaseQuoteSource_FixedSubscriptionTests : IDisposable
    {
        private readonly QuotesPartition _cut;

        public CoinbaseQuoteSource_FixedSubscriptionTests() =>
            _cut = CoinbaseQuoteSourceFactory.GetCoinbaseQuoteSource();

        public void Dispose()
        {
            _cut?.Dispose();
        }

        [Fact]
        public async Task SubscribeOnPairStream_JoinToEventStream_SoNextEventsAreSubsetOfEarilerSubscription()
        {
            const TradingPair productType = TradingPair.BtcUsd;
            var events1 = new List<OrderBookModifyiableEvent>();
            var events2 = new List<OrderBookModifyiableEvent>();
            var events3 = new List<OrderBookModifyiableEvent>();
            // act
            _cut.Streams(productType).Subscribe(e => events1.Add(e));
            await Task.Delay(1000);
            _cut.Streams(productType).Subscribe(e => events2.Add(e));
            await Task.Delay(1000);
            _cut.Streams(productType).Subscribe(e => events3.Add(e));
            await Task.Delay(1000);
            // assert
            events1.Should().NotBeEmpty();
            events2.Should().NotBeEmpty();
            events3.Should().NotBeEmpty();
            events2.Should().BeSubsetOf(events1);
            events3.Should().BeSubsetOf(events2);
        }

        [Fact]
        public async Task Streams_ReturnedObservableToGivenPair_StreamsExpectedPair()
        {
            var (pair1, pair2, pair3) = (TradingPair.BtcUsd, TradingPair.BtcUsdc, TradingPair.EthUsd);
            var events1 = new List<OrderBookModifyiableEvent>();
            var events2 = new List<OrderBookModifyiableEvent>();
            var events3 = new List<OrderBookModifyiableEvent>();
            // act
            _cut.Streams(pair1).Subscribe(e => events1.Add(e));
            await Task.Delay(1000);
            _cut.Streams(pair2).Subscribe(e => events2.Add(e));
            await Task.Delay(1000);
            _cut.Streams(pair3).Subscribe(e => events3.Add(e));
            await Task.Delay(1000);
            // assert
            events1.Should().NotBeEmpty();
            events2.Should().NotBeEmpty();
            events3.Should().NotBeEmpty();
            events1.All(x => x.TradingPair == pair1).Should().BeTrue();
            events2.All(x => x.TradingPair == pair2).Should().BeTrue();
            events3.All(x => x.TradingPair == pair3).Should().BeTrue();
        }
    }
}