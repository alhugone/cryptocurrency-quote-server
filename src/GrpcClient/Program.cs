using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcService;
using Newtonsoft.Json;

namespace GrpcClient
{
    public class Program
    {
        private static readonly Pair[] Pairs = Enum.GetValues<Pair>();
        private static readonly Random random = new();
        private static volatile Dictionary<Pair, int> _dict = new();

        public static async Task Main(string[] args)
        {
            var tasks = new List<Task>();
            using var channel = GrpcChannel.ForAddress("http://localhost:5000");
            for (var i = 0; i < 10; i++) tasks.Add(Run(channel));
            tasks.Add(RunSnapshots(channel));
            tasks.Add(RunSnapshots(channel));
            //      tasks.Add(Run(channel));
            tasks.Add(RunUpdatable(channel));
            await Task.WhenAll(tasks);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static async Task Run(GrpcChannel channel)
        {
            var client = new QuoteService.QuoteServiceClient(channel);
            var reply = client.SubscribeTo(new SubscribeToRequest {Pair = Pair.DashUsd});
            var responseStream = reply.ResponseStream;
            while (await responseStream.MoveNext(CancellationToken.None))
                Console.WriteLine(
                    $"Update:[{Thread.CurrentThread.ManagedThreadId}] " + responseStream.Current.InstrumentCase
                );
        }

        private static async Task RunSnapshots(GrpcChannel channel)
        {
            var client = new QuoteService.QuoteServiceClient(channel);
            do
            {
                var reply = await client.GetL3OrderBookSnapshotAsync(
                    new GetL3OrderBookSnapshotRequest {Pair = GetPair()}
                );
                Console.WriteLine(
                    $"Snapshot:[{Thread.CurrentThread.ManagedThreadId}]{Environment.NewLine} " +
                    JsonConvert.SerializeObject(reply)
                );
                await Task.Delay(random.Next(5000, 5000));
            } while (true);
        }

        private static async Task RunUpdatable(GrpcChannel channel)
        {
            var client = new QuoteService.QuoteServiceClient(channel);
            var call = client.UpdatableSubscribeTo();
            _dict = new Dictionary<Pair, int>();
            var readTask = Task.Run(
                async () =>
                {
                    await foreach (var response in call.ResponseStream.ReadAllAsync())
                    {
                        if (_dict.TryGetValue(response.Pair, out var cnt))
                            _dict[response.Pair] = ++cnt;
                        else _dict[response.Pair] = 1;
                        Console.WriteLine(
                            $"Update:[{Thread.CurrentThread.ManagedThreadId}] " +
                            JsonConvert.SerializeObject(_dict, Formatting.Indented)
                        );
                    }
                }
            );
            var request = new UpdatableSubscribeToRequest();
            while (true)
            {
                var pairs = GetPairs().ToArray();
                request.Pairs.Clear();
                request.Pairs.AddRange(pairs);
                Console.WriteLine(
                    "Updated pairs:" + JsonConvert.SerializeObject(pairs.Select(Enum.GetName), Formatting.Indented)
                );
                await call.RequestStream.WriteAsync(request);
                _dict = pairs.ToDictionary(x => x, x => 0);
                await Task.Delay(TimeSpan.FromSeconds(20));
            }
        }

        private static IEnumerable<Pair> GetPairs()
        {
            var count = random.Next(2, 4);
            var set = new HashSet<Pair>();
            for (var i = 0; i < count; i++)
                while (set.Add(GetPair()) != true)
                    ;
            return set;
        }

        private static Pair GetPair() => Pairs[random.Next(Pairs.Length)];
    }
}