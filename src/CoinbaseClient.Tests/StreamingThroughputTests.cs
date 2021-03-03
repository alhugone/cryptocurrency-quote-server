using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinbasePro.Shared.Types;
using CoinbasePro.WebSocket;
using CoinbasePro.WebSocket.Types;
using QuoteServer.OrderBook.Primitives;
using TestsInfrastructureHelpers;
using Xunit;
using Xunit.Abstractions;

namespace CoinbaseClient.Tests
{
    public class StreamingThroughputTests : IDisposable
    {
        private readonly IWebSocket _webSocket;
        private readonly ITestOutputHelper _output;

        public StreamingThroughputTests(ITestOutputHelper output)
        {
            _output = output;
            _webSocket = AuthenticatorFactory.GetCoinbaseProClient().WebSocket;
        }

        public void Dispose()
        {
        }

        [Theory]
        [InlineData(10, 01)]
        [InlineData(10, 02)]
        [InlineData(10, 04)]
        [InlineData(10, 08)]
        [InlineData(10, 12)]
        [InlineData(10, 16)]
        [InlineData(10, 16, true)]
        [InlineData(10, 50, true)]
        [InlineData(10, 100, true)]
        [InlineData(30, 1000, true)]
        public async Task WhenChangingSubscribedPairs_ReturnsAllOfThatPairsAndOnlyThatPairs(
            double time,
            int pairs,
            bool all = false)
        {
            var testRunTime = TimeSpan.FromSeconds(time);
            long counter = 0;
            Stopwatch? stopwatch = null;
            var semaphore = new SemaphoreSlim(0, 1);
            _webSocket.OnOpenReceived += (sender, args) => Interlocked.Increment(ref counter);
            _webSocket.OnDoneReceived += (sender, args) => Interlocked.Increment(ref counter);
            _webSocket.OnMatchReceived += (sender, args) => Interlocked.Increment(ref counter);
            _webSocket.OnChangeReceived += (sender, args) => Interlocked.Increment(ref counter);
            _webSocket.OnReceivedReceived += (sender, args) => Interlocked.Increment(ref counter);
            _webSocket.OnWebSocketOpenAndSubscribed += (sender, args) =>
            {
                semaphore.Release();
                stopwatch = Stopwatch.StartNew();
            };
            var source = all == false
                ? new[]
                {
                    ProductType.BtcUsdc, ProductType.AaveGbp, ProductType.EthBtc, ProductType.LtcBtc,
                    ProductType.EthUsdc, ProductType.LtcUsd, ProductType.BtcUsd, ProductType.AtomBtc,
                    ProductType.CompBtc, ProductType.EosBtc, ProductType.LinkBtc, ProductType.XrpBtc,
                    ProductType.AaveUsd, ProductType.EosUsd, ProductType.LinkUsd, ProductType.XrpUsd,
                }
                : Enum.GetValues<ProductType>();
            _webSocket.Start(source.Take(pairs).ToList(), new List<ChannelType> {ChannelType.Full});
            await semaphore.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(testRunTime);
            stopwatch!.Stop();
            _output.WriteLine($"Total events: {counter}, events per sec: {counter / stopwatch.Elapsed.TotalSeconds}");
            _webSocket.Stop();
        }
    }
}