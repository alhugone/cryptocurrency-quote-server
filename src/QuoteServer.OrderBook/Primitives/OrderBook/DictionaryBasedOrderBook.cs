using System;
using System.Collections.Generic;
using System.Linq;
using QuoteServer.OrderBook.Partition.Events;

namespace QuoteServer.OrderBook.Primitives.OrderBook
{
    public class DictionaryBasedOrderBook : IOrderBook
    {
        public DictionaryBasedOrderBook(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids)
        {
            ResetTo(sequence, asks, bids);
        }

        public DictionaryBasedOrderBook()
        {
        }

        private Dictionary<Guid, Quote> _asks { get; } = new(100_000);
        private Dictionary<Guid, Quote> _bids { get; } = new(100_000);
        public ISet<Quote> Asks => _asks.Values.ToHashSet();
        public ISet<Quote> Bids => _bids.Values.ToHashSet();
        public long Sequence { get; private set; }

        public void Apply(OrderBookModifyiableEvent @event)
        {
            switch (@event)
            {
                case Opened opened:
                    When(opened);
                    break;
                case Closed closed:
                    When(closed);
                    break;
                case Matched matched:
                    When(matched);
                    break;
                case Changed changed:
                    When(changed);
                    break;
                case ReceivedAny:
                    break;
                default:
                    throw new Exception();
            }
            Sequence = @event.Sequence;
        }

        public void ResetTo(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids)
        {
            _asks.Clear();
            _bids.Clear();
            Sequence = sequence;
            foreach (var quoteM in asks) _asks.Add(quoteM.OrderId, quoteM);
            foreach (var quoteM in bids) _bids.Add(quoteM.OrderId, quoteM);
        }

        private void When(Opened opened)
        {
            var quote = new Quote(opened.OrderId, opened.Price, opened.RemainingSize, opened.Sequence);
            switch (opened.Side)
            {
                case OrderSide.Buy:
                    _bids.Add(quote.OrderId, quote);
                    break;
                case OrderSide.Sell:
                    _asks.Add(quote.OrderId, quote);
                    break;
            }
        }

        private void When(Changed changed)
        {
            Quote? quote = null;
            switch (changed.Side)
            {
                case OrderSide.Buy:
                    if (_bids.TryGetValue(changed.OrderId, out quote))
                        quote.Size = changed.NewSize;
                    break;
                case OrderSide.Sell:
                    if (_asks.TryGetValue(changed.OrderId, out quote))
                        quote.Size = changed.NewSize;
                    break;
            }
        }

        private void When(Matched matched)
        {
            Quote? quote = null;
            switch (matched.Side)
            {
                case OrderSide.Buy:
                    if (_bids.TryGetValue(matched.MakerOrderId, out quote))
                        quote.Size -= matched.Size;
                    if (_asks.TryGetValue(matched.TakerOrderId, out quote))
                        quote.Size -= matched.Size;
                    if (!_bids.ContainsKey(matched.MakerOrderId) && !_asks.ContainsKey(matched.TakerOrderId))
                        throw new Exception("");
                    break;
                case OrderSide.Sell:
                    if (_bids.TryGetValue(matched.TakerOrderId, out quote))
                        quote.Size -= matched.Size;
                    if (_asks.TryGetValue(matched.MakerOrderId, out quote))
                        quote.Size -= matched.Size;
                    if (!_asks.ContainsKey(matched.MakerOrderId) && !_asks.ContainsKey(matched.TakerOrderId))
                        throw new Exception("");
                    break;
            }
        }

        private void When(Closed closed)
        {
            switch (closed.Side)
            {
                case OrderSide.Buy:
                    _bids.Remove(closed.OrderId);
                    break;
                case OrderSide.Sell:
                    _asks.Remove(closed.OrderId);
                    break;
            }
        }
    }
}