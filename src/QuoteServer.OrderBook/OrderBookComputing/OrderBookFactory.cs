using System.Collections.Generic;
using System.Linq;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.OrderBookComputing
{
    public static class OrderBookFactory
    {
        public static IOrderBook From(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids) =>
            new HashBasedOrderBook(sequence, asks, bids);

        public static IOrderBook From(OrderBookL3Snapshot snapshot) =>
            From(snapshot.Sequence, snapshot.Asks, snapshot.Bids);

        public static IOrderBook Empty() => From(0, Enumerable.Empty<Quote>(), Enumerable.Empty<Quote>());
    }
}