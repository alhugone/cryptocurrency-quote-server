using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using QuoteServer.OrderBook.OrderBookComputing;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;

namespace GrpcService.Services
{
    public class GrpcQuoteService : QuoteService.QuoteServiceBase
    {
        private readonly ILogger<GrpcQuoteService> _logger;
        private readonly OrderBooksEngine _orderBooksEngine;
        private readonly IQuotesPartition _quotesPartition;

        public GrpcQuoteService(
            ILogger<GrpcQuoteService> logger,
            IQuotesPartition quotesPartition,
            OrderBooksEngine orderBooksEngine)
        {
            _logger = logger;
            _quotesPartition = quotesPartition;
            _orderBooksEngine = orderBooksEngine;
        }

        public override async Task SubscribeTo(
            SubscribeToRequest request,
            IServerStreamWriter<OrderBookChanged> responseStream,
            ServerCallContext context)
        {
            var waitForStreamingEnd = new SemaphoreSlim(0);
            var _ = _orderBooksEngine.ManageOrderBookFor(Mapper.Map(request.Pair));
            var subscription = _quotesPartition.Streams(Mapper.Map(request.Pair))
                                               .Select(
                                                   orderBookModifyiableEvent =>
                                                   {
                                                       return Observable.FromAsync(
                                                           () => responseStream.WriteAsync(
                                                               Mapper.Map(orderBookModifyiableEvent)
                                                           )
                                                       );
                                                   }
                                               )
                                               .Concat()
                                               .Subscribe(
                                                   _ => { },
                                                   e => { _logger.LogError(e, "An error occured"); },
                                                   () => { waitForStreamingEnd.Release(); }
                                               );
            context.CancellationToken.Register(
                () =>
                {
                    subscription.Dispose();
                    waitForStreamingEnd.Release();
                }
            );
            await waitForStreamingEnd.WaitAsync();
        }

        public override async Task<L3OrderBookSnapshot> GetL3OrderBookSnapshot(
            GetL3OrderBookSnapshotRequest request,
            ServerCallContext context) =>
            //Mapper.Map(_orderBooksController.GetOrderBookL3Snapshot(Mapper.Map(request.Pair)));
            Mapper.Map(await _quotesPartition.GetOrderBookL3Snapshot(Mapper.Map(request.Pair)));

        public override async Task UpdatableSubscribeTo(
            IAsyncStreamReader<UpdatableSubscribeToRequest> requestStream,
            IServerStreamWriter<OrderBookChanged> responseStream,
            ServerCallContext context)
        {
            var pairsSubject = new Subject<TradingPair[]>();
            var _ = Task.Run(
                async () =>
                {
                    await foreach (var response in requestStream.ReadAllAsync())
                        pairsSubject.OnNext(Mapper.Map(response.Pairs));
                }
            );
            var waitForStreamingEnd = new SemaphoreSlim(0);
            var subscription = _quotesPartition.Streams(pairsSubject)
                                               .Select(
                                                   orderBookModifyiableEvent =>
                                                   {
                                                       return Observable.FromAsync(
                                                           () => responseStream.WriteAsync(
                                                               Mapper.Map(orderBookModifyiableEvent)
                                                           )
                                                       );
                                                   }
                                               )
                                               .Concat()
                                               .Subscribe(
                                                   _ => { },
                                                   e => { _logger.LogError(e, "An error occured"); },
                                                   () => { waitForStreamingEnd.Release(); }
                                               );
            context.CancellationToken.Register(
                () =>
                {
                    subscription.Dispose();
                    waitForStreamingEnd.Release();
                    pairsSubject.Dispose();
                }
            );
            await waitForStreamingEnd.WaitAsync();
        }
    }
}