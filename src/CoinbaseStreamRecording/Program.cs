using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Coinbase.QuoteSource;
using CoinbasePro.Services.Products.Types;
using CoinbasePro.Shared.Types;
using CoinbasePro.WebSocket.Types;
using CoinbaseStreamRecording.SessionRecorder;
using Newtonsoft.Json;
using TestsInfrastructureHelpers;

namespace CoinbaseStreamRecording
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            const ProductType productType = ProductType.BtcUsd;
            var coinbaseProClient = AuthenticatorFactory.GetCoinbaseProClient();
            var productTypes = new List<ProductType> {productType};
            var channels = new List<ChannelType> {ChannelType.Full};
            var webSocket = coinbaseProClient.WebSocket;
            var streamRecored = SessionRecorderFactory.GetStreamRecorder();
            webSocket.OnOpenReceived += (sender, args) => streamRecored.Record(CoinbaseTypeMapper.Map(args.LastOrder));
            webSocket.OnDoneReceived += (sender, args) => streamRecored.Record(CoinbaseTypeMapper.Map(args.LastOrder));
            webSocket.OnMatchReceived += (sender, args) => streamRecored.Record(CoinbaseTypeMapper.Map(args.LastOrder));
            webSocket.OnChangeReceived +=
                (sender, args) => streamRecored.Record(CoinbaseTypeMapper.Map(args.LastOrder));
            webSocket.OnReceivedReceived +=
                (sender, args) => streamRecored.Record(CoinbaseTypeMapper.Map(args.LastOrder));
            // act
            webSocket.Start(productTypes, channels);
            Thread.Sleep(1000); // fill with events before first OrderBookSnapshot
            var productOrderBook =
                await coinbaseProClient.ProductsService.GetProductOrderBookAsync(productType, ProductLevel.Three);
            if (productOrderBook.Sequence < streamRecored.MinSequence)
                throw new Exception("Not this time, some events are missing since the first snapshot");
            streamRecored.Record(CoinbaseTypeMapper.MapToL3Snapshot(productOrderBook));
            var randomRand = new Random();
            var stop = false;
            var t = new Task(
                () =>
                {
                    while (Console.ReadKey().Key != ConsoleKey.X)
                    {
                    }
                    stop = true;
                    Console.WriteLine("Stopping...");
                }
            );
            t.Start();
            while (!stop)
            {
                Thread.Sleep(randomRand.Next(100, 1000 * 10));
                productOrderBook =
                    await coinbaseProClient.ProductsService.GetProductOrderBookAsync(productType, ProductLevel.Three);
                streamRecored.Record(CoinbaseTypeMapper.MapToL3Snapshot(productOrderBook));
                Console.WriteLine($"Statistics {DateTimeOffset.Now.ToString()}");
                Console.WriteLine(JsonConvert.SerializeObject(streamRecored.Statistics, Formatting.Indented));
            }
            var sync = new AutoResetEvent(false);
            streamRecored.WhenSequence(productOrderBook.Sequence, () => { sync.Set(); });
            sync.WaitOne(); //wait for rest of events up to second snapshot
            webSocket.Stop();
            var pathToPersistedSession = streamRecored.PersistSession();
            Console.WriteLine("stopped");
            // assert
        }
    }
}