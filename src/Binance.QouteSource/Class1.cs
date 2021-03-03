using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace Binance.QouteSource
{
    public class Class1: IQuotesSourceConnection
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingPair) => throw new NotImplementedException();

        public void SetSubscribedPairs(IEnumerable<TradingPair> pairs)
        {
            throw new NotImplementedException();
        }

        public event Action<OrderBookModifyiableEvent> OnEvent;
        public Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingPair) => throw new NotImplementedException();
    }
}