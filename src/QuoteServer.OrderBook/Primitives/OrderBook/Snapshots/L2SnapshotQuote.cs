namespace QuoteServer.OrderBook.Primitives.OrderBook.Snapshots
{
    public class L2SnapshotQuote
    {
        public L2SnapshotQuote(decimal price, decimal size, long ordersCount)
        {
            Price = price;
            Size = size;
            OrdersCount = ordersCount;
        }

        public L2SnapshotQuote()
        {
        }

        public decimal Price { get; init; }
        public decimal Size { get; set; }
        public long OrdersCount { get; init; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Price.Equals(((L2SnapshotQuote) obj).Price);
        }

        public override int GetHashCode() => Price.GetHashCode();
    }
}