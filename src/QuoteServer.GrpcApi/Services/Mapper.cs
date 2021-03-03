using System;
using System.Linq;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace GrpcService.Services
{
    public static class Mapper
    {
        public static TradingPair Map(Pair pair) =>
            pair switch
            {
                Pair.AtomBtc => TradingPair.AtomBtc,
                Pair.BtcUsdc => TradingPair.BtcUsdc,
                Pair.BtcUsdt => TradingPair.BtcUsd,
                Pair.DashUsd => TradingPair.DashUsd,
                Pair.EthDai => TradingPair.EthDai,
                Pair.EthUsd => TradingPair.EthUsd,
                Pair.XrpBtc => TradingPair.XrpBtc,
                _ => throw new ArgumentOutOfRangeException(nameof(Pair), pair, "Not supported"),
            };

        public static OrderBookChanged Map(OrderBookModifyiableEvent @event)
        {
            var hello = new OrderBookChanged
            {
                Pair = Map(@event.TradingPair),
                Price = @event.Price,
                Sequence = @event.Sequence,
                Timestamp = @event.Time.ToTimestamp(),
            };
            switch (@event)
            {
                case QuoteServer.OrderBook.Partition.Events.Changed changed:
                    hello.Changed = new Changed {Id = changed.OrderId.ToString(), NewSize = changed.NewSize};
                    break;
                case QuoteServer.OrderBook.Partition.Events.Closed opened:
                    hello.Closed = new Closed
                    {
                        Id = opened.OrderId.ToString(),
                        Reason = opened.Reason == DoneReasonType.Canceled ? Reason.Canceled : Reason.Filled,
                    };
                    break;
                case QuoteServer.OrderBook.Partition.Events.Opened opened:
                    hello.Opened = new Opened {Id = opened.OrderId.ToString(), Size = opened.RemainingSize};
                    break;
                case QuoteServer.OrderBook.Partition.Events.Matched opened:
                    hello.Matched = new Matched
                    {
                        MakerOrderId = opened.MakerOrderId.ToString(),
                        TakerOrderId = opened.TakerOrderId.ToString(),
                        Size = opened.Size,
                    };
                    break;
            }
            return hello;
        }

        private static Pair Map(TradingPair tradingPair)
        {
            return tradingPair switch
            {
                TradingPair.AtomBtc => Pair.AtomBtc,
                TradingPair.BtcUsdc => Pair.BtcUsdc,
                TradingPair.BtcUsd => Pair.BtcUsdt,
                TradingPair.DashUsd => Pair.DashUsd,
                TradingPair.EthDai => Pair.EthDai,
                TradingPair.EthUsd => Pair.EthUsd,
                TradingPair.XrpBtc => Pair.XrpBtc,
                _ => throw new ArgumentOutOfRangeException(nameof(Pair), tradingPair, "Not supported")
            };
        }

        public static L3OrderBookSnapshot Map(OrderBookL3Snapshot snapshot)
        {
            return new()
            {
                Sequence = snapshot.Sequence,
                Asks =
                {
                    snapshot.Asks.Select(
                        x => new Order {Id = x.OrderId.ToString(), Price = x.Price, Size = x.Size}
                    ),
                },
                Bids =
                {
                    snapshot.Asks.Select(
                        x => new Order {Id = x.OrderId.ToString(), Price = x.Price, Size = x.Size}
                    ),
                },
            };
        }

        public static TradingPair[] Map(RepeatedField<Pair> currentPairs) => currentPairs.Select(Map).ToArray();
    }
}