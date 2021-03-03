using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Interceptors;
using QuoteServer.OrderBook.Primitives;
using Xunit;

namespace CoinbaseClient.Tests
{
    public class JoinMultipleStreamsTests
    {
        [Fact]
        public void ShouldCallGetStreamForEachUniquePairInStream()
        {
            var pairs = new[]
            {
                new[] {TradingPair.AaveBtc, TradingPair.AaveUsd}, new[] {TradingPair.AaveEur},
                new[] {TradingPair.ZecUsdc, TradingPair.FilBtc, TradingPair.AlgoUsd},
            };
            var list = new List<TradingPair>();
            // act
            new JoinMultipleStreamsInterceptor(
                pairs.ToObservable(),
                x =>
                {
                    list.Add(x);
                    return Observable.Never<OrderBookModifyiableEvent>();
                }
            );
            // assert
            list.Should().BeEquivalentTo(pairs.SelectMany(x => x));
        }

        [Fact]
        public void WhenNewPairsDoesNotContainAnyPreviousPair_ShouldDisposeSubscriptionForThem()
        {
            var pairs = new[]
            {
                new[]
                {
                    new TestDisposable(TradingPair.AaveBtc), new TestDisposable(TradingPair.AaveUsd),
                    new TestDisposable(TradingPair.EosEur),
                },
                new[] {new TestDisposable(TradingPair.AaveEur), new TestDisposable(TradingPair.AaveUsd)},
            };
            // act
            new JoinMultipleStreamsInterceptor(
                pairs.Select(x => x.Select(y => y.TradingPair).ToArray()).ToObservable(),
                x =>
                {
                    var disp = pairs.SelectMany(x => x).First(y => y.TradingPair == x);
                    return Observable.Using(() => disp, _ => Observable.Never<OrderBookModifyiableEvent>());
                }
            );
            // assert
            var expectedDisposed = pairs.SelectMany(x => x).Where(y => y == pairs[0][0] || y == pairs[0][2]);
            expectedDisposed.All(x => x.Disposed).Should().BeTrue();
            pairs.SelectMany(x => x).Except(expectedDisposed).All(x => !x.Disposed).Should().BeTrue();
        }

        private class TestDisposable : IDisposable
        {
            public TestDisposable(TradingPair tradingPair) => TradingPair = tradingPair;
            public TradingPair TradingPair { get; }
            public bool Disposed { get; set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}