using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Exceptions;
using QuoteServer.OrderBook.Partition.Interceptors;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.Partition
{
    public class CombinedQuotesPartitions : IQuotesPartition
    {
        private readonly Dictionary<TradingPair, IQuotesPartition> _partitionsMap;

        public CombinedQuotesPartitions(
            IEnumerable<(IQuotesPartition partition, ISet<TradingPair> assignedPairs)> partitions)
        {
            var all = partitions.SelectMany(x => x.assignedPairs).ToList();
            if (all.Count != all.Distinct().Count())
                throw new PartitionsMustBeMutuallyExclusiveByAssignedPairs();
            _partitionsMap = partitions.SelectMany(tuple => tuple.assignedPairs.Select(pair => (pair, tuple.partition)))
                                       .ToDictionary(tuple => tuple.pair, x => x.partition);
        }

        public IObservable<OrderBookModifyiableEvent> Streams(TradingPair tradingTradingPair)
        {
            if (!_partitionsMap.ContainsKey(tradingTradingPair))
                throw new PartitionDoNotHandleTradingPair(tradingTradingPair);
            return _partitionsMap[tradingTradingPair].Streams(tradingTradingPair);
        }

        public IObservable<OrderBookModifyiableEvent> Streams(IObservable<TradingPair[]> paris) =>
            new JoinMultipleStreamsInterceptor(paris, Streams);

        public void Dispose()
        {
            foreach (var partition in _partitionsMap.Values.Distinct())
                partition.Dispose();
        }

        public Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingTradingPair)
        {
            if (!_partitionsMap.ContainsKey(tradingTradingPair))
                throw new PartitionDoNotHandleTradingPair(tradingTradingPair);
            return _partitionsMap[tradingTradingPair].GetOrderBookL3Snapshot(tradingTradingPair);
        }

        public Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingTradingPair)
        {
            if (!_partitionsMap.ContainsKey(tradingTradingPair))
                throw new PartitionDoNotHandleTradingPair(tradingTradingPair);
            return _partitionsMap[tradingTradingPair].GetOrderBookL2Snapshot(tradingTradingPair);
        }
    }
}