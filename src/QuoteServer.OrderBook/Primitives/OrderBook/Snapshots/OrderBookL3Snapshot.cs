using System;
using System.Collections.Generic;

namespace QuoteServer.OrderBook.Primitives.OrderBook.Snapshots
{
    public class OrderBookL3Snapshot
    {
        public OrderBookL3Snapshot()
        {
        }

        public OrderBookL3Snapshot(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids)
        {
            Sequence = sequence;
            Asks = new List<Quote>(asks);
            Bids = new List<Quote>(bids);
        }

        public OrderBookL3Snapshot(long sequence, Dictionary<Guid, Quote> asks, Dictionary<Guid, Quote> bids) : this(
            sequence,
            asks.Values,
            bids.Values
        )
        {
        }

        public List<Quote> Asks { get; } = new();
        public List<Quote> Bids { get; } = new();
        public long Sequence { get; }
    }
}