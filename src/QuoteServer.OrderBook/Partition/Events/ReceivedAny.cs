using System;

namespace QuoteServer.OrderBook.Partition.Events
{
    public class ReceivedAny : OrderBookModifyiableEvent
    {
        public decimal Size { get; set; }
        public Guid OrderId { get; set; }
    }
}