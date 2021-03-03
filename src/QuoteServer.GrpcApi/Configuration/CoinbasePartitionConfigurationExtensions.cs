using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using QuoteServer.OrderBook.Primitives;

namespace GrpcService.Configuration
{
    public static class CoinbasePartitionConfigurationExtensions
    {
        public static ISet<TradingPair> GetParisAsEnum(
            this PartitionsConfiguration.CoinbasePartitionConfiguration config)
        {
            if (config.OnlyPairs.Any() && config.AllExcept.Any())
                throw new Exception(
                    $"Invalid Partition configuration. Only one of {{{nameof(config.OnlyPairs)}, {config.AllExcept}}} can be used."
                );
            if (config.OnlyPairs.Any())
                return config.OnlyPairs.Select(Enum.Parse<TradingPair>).ToHashSet();
            if (config.AllExcept.Any())
                return Enum.GetValues<TradingPair>()
                           .Except(config.AllExcept.Select(Enum.Parse<TradingPair>))
                           .ToHashSet();
            return ImmutableHashSet<TradingPair>.Empty;
        }
    }
}