using System.Collections.Generic;
using QuoteServer.OrderBook.Partition.Events;

namespace QuoteServer.OrderBook.Primitives.OrderBook
{
    public interface IOrderBook
    {
        public ISet<Quote> Asks { get; }
        public ISet<Quote> Bids { get; }
        long Sequence { get; }
        void Apply(OrderBookModifyiableEvent @event);
        void ResetTo(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids);
    }
}