using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Primitives;
using Xunit;

namespace CoinbaseClient.Tests
{
    public class CoinbaseQuoteSource_UpdatableSubscriptionTests : IDisposable
    {
        private static readonly Random _random = new(1);
        private readonly QuotesPartition _cut;
        private volatile TradingPair[] _expected = Array.Empty<TradingPair>();
        private volatile bool _switchingPairs;

        public CoinbaseQuoteSource_UpdatableSubscriptionTests() =>
            _cut = CoinbaseQuoteSourceFactory.GetCoinbaseQuoteSource();

        public void Dispose()
        {
            _cut?.Dispose();
        }

        [Fact]
        [Description("NonDeterministic. Can fail when some events will not be recieved at expected wiat-duration.")]
        public async Task WhenChangingSubscribedPairs_ReturnsAllOfThatPairsAndOnlyThatPairs()
        {
            _expected = Array.Empty<TradingPair>();
            var allRecievedEventsCount = new ConcurrentDictionary<TradingPair, int>();
            var prevState = new Dictionary<TradingPair, int>();
            var pairsObservable = new Subject<TradingPair[]>();
            pairsObservable.OnNext(new TradingPair[0]);
            using var subscription = _cut.Streams(pairsObservable)
                                         .Subscribe(
                                             @event =>
                                             {
                                                 if (_switchingPairs == false)
                                                     allRecievedEventsCount[@event.TradingPair] =
                                                         allRecievedEventsCount[@event.TradingPair] + 1;
                                             }
                                         );
            for (var i = 0; i < 10; i++)
            {
                // act
                var snapshot = await Switch(Generate());
                // assert
                AssertThatAnyNonExpectedPairWasNotRecieved(prevState, snapshot.state, snapshot.expected);
                AssertThatAllExpectedPairsWasRecievedAtLeastOnce(prevState, snapshot.state, snapshot.expected);
                prevState = snapshot.state;
            }

            async Task<(Dictionary<TradingPair, int> state, TradingPair[] expected)> Switch(TradingPair[] switchTo)
            {
                _switchingPairs = true;
                var stateSnapshot = new Dictionary<TradingPair, int>(allRecievedEventsCount);
                var expectedSnapshot = _expected.ToArray();
                _expected = switchTo.Select(
                                        x =>
                                        {
                                            if (!allRecievedEventsCount.ContainsKey(x)) allRecievedEventsCount[x] = 0;
                                            return x;
                                        }
                                    )
                                    .ToArray();
                pairsObservable.OnNext(switchTo);
                await Task.Delay(500);
                _switchingPairs = false;
                await Task.Delay(_expected.Length * 1000);
                return (stateSnapshot, expectedSnapshot);
            }

            allRecievedEventsCount.Should().NotBeEmpty();
            // assert
        }

        private static TradingPair[] Generate()
        {
            var fixeda = new[]
            {
                TradingPair.BtcUsdc, TradingPair.AaveGbp, TradingPair.EthBtc, TradingPair.EthUsd,
                TradingPair.EthUsdc, TradingPair.EthBtc, TradingPair.BtcUsd, TradingPair.AtomBtc,
                TradingPair.CompBtc, TradingPair.EosBtc,
            };
            var set = new HashSet<TradingPair>();
            var count = _random.Next(0, fixeda.Length);
            while (set.Count < count) set.Add(fixeda[_random.Next(0, fixeda.Length)]);
            return set.ToArray();
        }

        private void AssertThatAnyNonExpectedPairWasNotRecieved(
            Dictionary<TradingPair, int> prevState,
            Dictionary<TradingPair, int> currentState,
            TradingPair[] expectedPairsToReceive)
        {
            prevState.Keys.Should().BeSubsetOf(currentState.Keys);
            foreach (var currentPairCount in currentState.Where(x => !expectedPairsToReceive.Contains(x.Key)))
                prevState[currentPairCount.Key].Should().Be(currentPairCount.Value);
        }

        private void AssertThatAllExpectedPairsWasRecievedAtLeastOnce(
            Dictionary<TradingPair, int> prevState,
            Dictionary<TradingPair, int> currentState,
            TradingPair[] expectedPairsToReceive)
        {
            foreach (var currentPairCount in currentState.Where(x => expectedPairsToReceive.Contains(x.Key)))
            {
                currentPairCount.Value.Should().BeGreaterThan(0);
                if (prevState.ContainsKey(currentPairCount.Key))
                    currentPairCount.Value.Should().BeGreaterThan(prevState[currentPairCount.Key]);
            }
        }
    }
}