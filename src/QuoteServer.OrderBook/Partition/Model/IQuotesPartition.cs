using System;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.Partition.Model
{
    public interface IQuotesPartition : IDisposable
    {
        Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingTradingPair);
        Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingTradingPair);
        IObservable<OrderBookModifyiableEvent> Streams(TradingPair tradingTradingPair);
        IObservable<OrderBookModifyiableEvent> Streams(IObservable<TradingPair[]> pairs);
    }
}