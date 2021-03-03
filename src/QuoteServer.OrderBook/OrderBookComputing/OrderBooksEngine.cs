using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;
using static QuoteServer.OrderBook.OrderBookComputing.OrderBookSynchronizer.SynchronizationResult;

namespace QuoteServer.OrderBook.OrderBookComputing
{
    public class OrderBooksEngine : IDisposable
    {
        private readonly Dictionary<TradingPair, OrderBookSynchronizer> _orderBooks = new();
        private readonly IQuotesPartition _quotesPartition;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
        public OrderBooksEngine(IQuotesPartition quotesPartition) => _quotesPartition = quotesPartition;
        public void Dispose() => _quotesPartition.Dispose();

        public async Task ManageOrderBookFor(TradingPair tradingPair)
        {
            if (!_orderBooks.ContainsKey(tradingPair))
            {
                await _semaphoreSlim.WaitAsync();
                try
                {
                    if (!_orderBooks.ContainsKey(tradingPair))
                        _orderBooks.Add(tradingPair, await CreateOrderBook(tradingPair, _quotesPartition));
                }
                finally
                {
                    _semaphoreSlim.Release();
                }
            }
        }

        private static async Task<OrderBookSynchronizer> CreateOrderBook(
            TradingPair tradingPair,
            IQuotesPartition quotesPartition)
        {
            OrderBookL3Snapshot? snapshot = null;
            var orderBook = OrderBookFactory.From(0L, Enumerable.Empty<Quote>(), Enumerable.Empty<Quote>());
            var orderBookSynchronizer = new OrderBookSynchronizer(orderBook, quotesPartition.Streams(tradingPair));
            do
            {
                snapshot = await quotesPartition.GetOrderBookL3Snapshot(tradingPair);
            } while (orderBookSynchronizer.SynchronizeOrderBookInitialStateWith(snapshot) ==
                     SnapshotHasBeenTakenTooEarlyIHaveNoEventsWithThatSequence);
            return orderBookSynchronizer;
        }

        public OrderBookL3Snapshot GetOrderBookL3Snapshot(TradingPair tradingPair) =>
            _orderBooks[tradingPair].TakeOrderBookL3Snapshot();
    }
}