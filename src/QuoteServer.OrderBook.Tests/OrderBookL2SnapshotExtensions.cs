using System.Linq;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.Tests
{
    public static class OrderBookL2SnapshotExtensions
    {
        public static OrderBookL2Snapshot TrimToTop50(this OrderBookL2Snapshot snapshot) =>
            new(snapshot.Sequence, snapshot.Asks.Take(50), snapshot.Bids.Take(50));
    }
}