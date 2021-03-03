using System;
using QuoteServer.OrderBook.Primitives;

namespace QuoteServer.OrderBook.Partition.Events
{
    public class OrderBookModifyiableEvent
    {
        public OrderSide Side { get; set; }
        public decimal Price { get; set; }
        public TradingPair TradingPair { get; set; }
        public long Sequence { get; set; }
        public DateTimeOffset Time { get; set; }
    }
}