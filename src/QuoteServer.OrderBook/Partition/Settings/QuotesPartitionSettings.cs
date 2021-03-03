using System;
using System.Collections.Generic;
using QuoteServer.OrderBook.Primitives;

namespace QuoteServer.OrderBook.Partition.Settings
{
    public class QuotesPartitionSettings
    {
        public QuotesPartitionSettings() => TradingPairs = new HashSet<TradingPair>(Enum.GetValues<TradingPair>());
        public int NumberOfReaders { get; init; } = 1;
        public ISet<TradingPair> TradingPairs { get; init; }
        public TimeSpan DelayUpdatingConnectionWhenNoSubscribersOnPairTo { get; init; } = TimeSpan.FromSeconds(2);
    }
}