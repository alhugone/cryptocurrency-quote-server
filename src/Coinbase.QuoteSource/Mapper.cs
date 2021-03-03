using System;
using System.Linq;
using CoinbasePro.Services.Products.Models.Responses;
using CoinbasePro.Shared.Types;
using CoinbasePro.WebSocket.Models.Response;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace Coinbase.QuoteSource
{
    public static class CoinbaseTypeMapper
    {
        public static OrderBookL3Snapshot MapToL3Snapshot(ProductsOrderBookResponse responses)
        {
            return new(
                (long) responses.Sequence,
                responses.Asks.Select(x => new Quote {Price = x.Price, Size = x.Size, OrderId = x.OrderId!.Value})
                         .ToList(),
                responses.Bids.Select(x => new Quote {Price = x.Price, Size = x.Size, OrderId = x.OrderId!.Value})
                         .ToList()
            );
        }
        
        public static OrderBookL2Snapshot MapToL2Snapshot(ProductsOrderBookResponse responses)
        {
            return new(
                (long) responses.Sequence,
                responses.Asks.Select(x => new L2SnapshotQuote() {Price = x.Price, Size = x.Size, OrdersCount = (long) x.NumberOfOrders!.Value})
                         .ToList(),
                responses.Bids.Select(x => new L2SnapshotQuote {Price = x.Price, Size = x.Size, OrdersCount = (long) x.NumberOfOrders!.Value})
                         .ToList()
            );
        }

        public static ProductType Map(TradingPair tradingPair) => (ProductType) tradingPair;

        public static OrderBookModifyiableEvent Map(BaseMessage message, Cache? cache = null)
        {
            switch (message)
            {
                case Open open:
                    var opened = cache?.Opened ?? new Opened();
                    opened.Price = open.Price;
                    opened.Sequence = open.Sequence;
                    opened.Side = (OrderSide) open.Side;
                    opened.Time = open.Time;
                    opened.OrderId = open.OrderId;
                    opened.TradingPair = (TradingPair) open.ProductId;
                    opened.RemainingSize = open.RemainingSize;
                    return opened;
                case Done close:
                    var closed = cache?.Closed ?? new Closed();
                    closed.Price = close.Price;
                    closed.Sequence = close.Sequence;
                    closed.Side = (OrderSide) close.Side;
                    closed.Time = close.Time;
                    closed.OrderId = close.OrderId;
                    closed.TradingPair = (TradingPair) close.ProductId;
                    closed.RemainingSize = close.RemainingSize;
                    closed.Reason = (DoneReasonType) close.Reason;
                    return closed;
                case Match match:
                    var matched = cache?.Matched ?? new Matched();
                    matched.Price = match.Price;
                    matched.Sequence = match.Sequence;
                    matched.Side = (OrderSide) match.Side;
                    matched.Time = match.Time;
                    matched.MakerOrderId = match.MakerOrderId;
                    matched.TakerOrderId = match.TakerOrderId;
                    matched.TradingPair = (TradingPair) match.ProductId;
                    matched.Size = match.Size;
                    return matched;
                case Change change:
                    var changed = cache?.Changed ?? new Changed();
                    changed.Price = change.Price;
                    changed.Sequence = change.Sequence;
                    changed.Side = (OrderSide) change.Side;
                    changed.Time = change.Time;
                    changed.NewSize = change.NewSize;
                    changed.OrderId = change.OrderId;
                    changed.TradingPair = (TradingPair) change.ProductId;
                    return changed;
                case Received received:
                    var receivedAny = cache?.ReceivedAny ?? new ReceivedAny();
                    receivedAny.Price = received.Price;
                    receivedAny.Sequence = received.Sequence;
                    receivedAny.Side = (OrderSide) received.Side;
                    receivedAny.Time = received.Time;
                    receivedAny.Size = received.Size;
                    receivedAny.OrderId = received.OrderId;
                    receivedAny.TradingPair = (TradingPair) received.ProductId;
                    return receivedAny;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message.GetType(), null);
            }
        }

        public class Cache
        {
            public Opened Opened { get;  } = new Opened();
            public Closed Closed { get;  } = new();
            public Matched Matched { get;  }= new();
            public Changed Changed { get; }= new();
            public ReceivedAny ReceivedAny { get;  }= new();
        }
    }
}