using System.Linq;

namespace QuoteServer.OrderBook.Primitives.OrderBook.Snapshots
{
    public class OrderBookSnapshotsFactory
    {
        public static OrderBookL3Snapshot L3Snapshot(IOrderBook orderBook) =>
            new(orderBook.Sequence, SortAsks(orderBook), SortBids(orderBook));

        public static OrderBookL2Snapshot L2Snapshot(IOrderBook orderBook) =>
            new(orderBook.Sequence, SortAsks(orderBook), SortBids(orderBook));

        private static IOrderedEnumerable<Quote> SortBids(IOrderBook orderBook)
        {
            return orderBook.Bids.OrderByDescending(x => x.Price).ThenBy(x => x.Sequence);
        }

        private static IOrderedEnumerable<Quote> SortAsks(IOrderBook orderBook)
        {
            return orderBook.Asks.OrderBy(x => x.Price).ThenBy(x => x.Sequence);
        }
    }
}