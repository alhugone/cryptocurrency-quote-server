using System;

namespace QuoteServer.OrderBook.Primitives
{
    public class Quote
    {
        public Quote(Guid orderId, decimal price, decimal size, long sequence)
        {
            OrderId = orderId;
            Price = price;
            Size = size;
            Sequence = sequence;
        }

        public Quote()
        {
        }

        public decimal Price { get; init; }
        public decimal Size { get; set; }
        public Guid OrderId { get; set; }
        public long Sequence { get; init; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return OrderId.Equals(((Quote) obj).OrderId);
        }

        public override int GetHashCode() => OrderId.GetHashCode();
    }
}