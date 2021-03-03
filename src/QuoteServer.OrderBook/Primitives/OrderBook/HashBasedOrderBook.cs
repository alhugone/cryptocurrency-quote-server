using System;
using System.Collections.Generic;
using QuoteServer.OrderBook.Partition.Events;

namespace QuoteServer.OrderBook.Primitives.OrderBook
{
    public class HashBasedOrderBook : IOrderBook
    {
        private readonly Quote _searchQuote = new();

        public HashBasedOrderBook(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids)
        {
            Sequence = sequence;
            foreach (var quoteM in asks) _asks.Add(quoteM);
            foreach (var quoteM in bids) _bids.Add(quoteM);
        }

        public HashBasedOrderBook()
        {
        }

        private HashSet<Quote> _asks { get; } = new();
        private HashSet<Quote> _bids { get; } = new();

        public void ResetTo(long sequence, IEnumerable<Quote> asks, IEnumerable<Quote> bids)
        {
            _asks.Clear();
            _bids.Clear();
            Sequence = sequence;
            foreach (var quoteM in asks) _asks.Add(quoteM);
            foreach (var quoteM in bids) _bids.Add(quoteM);
        }

        public ISet<Quote> Asks => _asks;
        public ISet<Quote> Bids => _bids;
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

        public void When(Opened opened)
        {
            var quote = new Quote(opened.OrderId, opened.Price, opened.RemainingSize, opened.Sequence);
            switch (opened.Side)
            {
                case OrderSide.Buy:
                    _bids.Add(quote);
                    break;
                case OrderSide.Sell:
                    _asks.Add(quote);
                    break;
            }
        }

        private void When(Changed changed)
        {
            _searchQuote.OrderId = changed.OrderId;
            Quote? quote = null;
            switch (changed.Side)
            {
                case OrderSide.Buy:
                    if (_bids.TryGetValue(_searchQuote, out quote))
                        quote.Size = changed.NewSize;
                    break;
                case OrderSide.Sell:
                    if (_asks.TryGetValue(_searchQuote, out quote))
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
                    _searchQuote.OrderId = matched.MakerOrderId;
                    if (_bids.TryGetValue(_searchQuote, out quote))
                        quote.Size -= matched.Size;
                    _searchQuote.OrderId = matched.TakerOrderId;
                    if (_asks.TryGetValue(_searchQuote, out quote))
                        quote.Size -= matched.Size;
                    break;
                case OrderSide.Sell:
                    _searchQuote.OrderId = matched.TakerOrderId;
                    if (_bids.TryGetValue(_searchQuote, out quote))
                        quote.Size -= matched.Size;
                    _searchQuote.OrderId = matched.MakerOrderId;
                    if (_asks.TryGetValue(_searchQuote, out quote))
                        quote.Size -= matched.Size;
                    break;
            }
        }

        private void When(Closed closed)
        {
            _searchQuote.OrderId = closed.OrderId;
            switch (closed.Side)
            {
                case OrderSide.Buy:
                    _bids.Remove(_searchQuote);
                    break;
                case OrderSide.Sell:
                    _asks.Remove(_searchQuote);
                    break;
            }
        }
    }
}