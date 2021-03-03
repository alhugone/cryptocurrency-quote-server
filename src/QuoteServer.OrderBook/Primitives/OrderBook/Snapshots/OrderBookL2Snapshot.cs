using System;
using System.Collections.Generic;
using System.Linq;
using QuoteServer.OrderBook.Primitives.OrderBook.Exceptions;

namespace QuoteServer.OrderBook.Primitives.OrderBook.Snapshots
{
    public class OrderBookL2Snapshot
    {
        public OrderBookL2Snapshot()
        {
        }

        public OrderBookL2Snapshot(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids) : this(
            sequence,
            asks.GroupBy(x => x.Price)
                .Select(x => new L2SnapshotQuote(x.Key, x.Sum(y => y.Size), x.Count()))
                .OrderBy(x => x.Price)
                .ToList(),
            bids.GroupBy(x => x.Price)
                .Select(x => new L2SnapshotQuote(x.Key, x.Sum(y => y.Size), x.Count()))
                .OrderByDescending(x => x.Price)
                .ToList()
        )
        {
        }

        public OrderBookL2Snapshot(long sequence, IEnumerable<L2SnapshotQuote> asks, IEnumerable<L2SnapshotQuote> bids)
        {
            Sequence = sequence;
            Asks = asks.ToList();
            Bids = bids.ToList();
            if (Asks.FirstOrDefault()?.Price <= Bids.FirstOrDefault()?.Price)
                throw new BidsAndAsksOverlaps(Asks.First().Price, Bids.First().Price);
        }

        public OrderBookL2Snapshot(long sequence, Dictionary<Guid, Quote> asks, Dictionary<Guid, Quote> bids) : this(
            sequence,
            asks.Values,
            bids.Values
        )
        {
        }

        public List<L2SnapshotQuote> Asks { get; } = new();
        public List<L2SnapshotQuote> Bids { get; } = new();
        public long Sequence { get; }
    }
}