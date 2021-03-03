using System;

namespace QuoteServer.OrderBook.Partition.Events
{
    public class StateEvent : OrderBookModifyiableEvent
    {
        public Guid OrderId { get; set; }
        public decimal RemainingSize { get; set; }
    }
}