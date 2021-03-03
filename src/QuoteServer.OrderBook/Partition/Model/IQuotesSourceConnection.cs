using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.Partition.Model
{
    public interface IQuotesSourceConnection : IDisposable
    {
        Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingPair);
        void SetSubscribedPairs(IEnumerable<TradingPair> pairs);
        public event Action<OrderBookModifyiableEvent> OnEvent;
        Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingPair);
    }
}