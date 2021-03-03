using System;

namespace QuoteServer.OrderBook.Partition.Events
{
    public class Changed : OrderBookModifyiableEvent
    {
        public decimal NewSize { get; set; }
        public Guid OrderId { get; set; }
    }
}