using System;

namespace QuoteServer.OrderBook.Partition.Events
{
    public class Matched : OrderBookModifyiableEvent
    {
        public Guid MakerOrderId { get; set; }
        public Guid TakerOrderId { get; set; }
        public decimal Size { get; set; }
    }
}