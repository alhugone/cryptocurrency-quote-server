using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Partition.Settings;
using QuoteServer.OrderBook.Primitives;

namespace GrpcService.Configuration
{
    public static class PartitionsBuilder
    {
        public static IServiceCollection AddPartitions(
            this IServiceCollection serviceCollection,
            PartitionsConfiguration configuration)
        {
            switch (configuration.Partitions?.Count)
            {
                case 0:
                    throw new ArgumentException("No partitions in configuration. Define at least one.");
                case 1:
                    var pairs = configuration.Partitions[0].GetParisAsEnum();
                    serviceCollection.AddSingleton<IQuotesPartition>(
                        serviceProvider => new QuotesPartition(
                            serviceProvider.GetRequiredService<IQuotesSourceConnection>(),
                            new QuotesPartitionSettings {TradingPairs = pairs}
                        )
                    );
                    break;
                default:
                    serviceCollection.AddSingleton<IQuotesPartition>(
                        services =>
                        {
                            var partitions =
                                new List<(IQuotesPartition partition, ISet<TradingPair> assignedPairs)>(
                                    configuration.Partitions!.Count
                                );
                            foreach (var partitionConfiguration in configuration.Partitions)
                            {
                                var parisAsEnum = partitionConfiguration.GetParisAsEnum();
                                partitions.Add(
                                    (new QuotesPartition(services.GetRequiredService<IQuotesSourceConnection>(), new QuotesPartitionSettings {TradingPairs = parisAsEnum}),
                                        parisAsEnum)
                                );
                            }
                            return new CombinedQuotesPartitions(partitions);
                        }
                    );
                    break;
            }
            return serviceCollection;
        }
    }
}