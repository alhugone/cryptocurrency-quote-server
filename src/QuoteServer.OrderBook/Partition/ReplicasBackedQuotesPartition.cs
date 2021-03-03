using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;
using QuoteServer.OrderBook.Partition.Exceptions;
using QuoteServer.OrderBook.Partition.Interceptors;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using QuoteServer.OrderBook.Primitives.OrderBook.Snapshots;

namespace QuoteServer.OrderBook.Partition
{
    public class ReplicasBackedQuotesPartition : IQuotesPartition
    {
        private readonly ISet<IQuotesPartition> _partitionsReplicas;
        private readonly ISet<TradingPair> _replicaTradingPairs;
        private readonly int SupportedDeduplicationBasedOnLastNEvents = 100;

        public ReplicasBackedQuotesPartition(
            ISet<IQuotesPartition> partitionsReplicas,
            ISet<TradingPair> replicaTradingPairs)
        {
            _partitionsReplicas = partitionsReplicas;
            _replicaTradingPairs = replicaTradingPairs;
        }

        public IObservable<OrderBookModifyiableEvent> Streams(TradingPair tradingTradingPair)
        {
            ThrowIfNotHandlingTradingPair(tradingTradingPair);
            var streams = _partitionsReplicas.Select(partition => partition.Streams(tradingTradingPair));
            return new DeduplcateEventsBySequenceInterceptor(streams, SupportedDeduplicationBasedOnLastNEvents);
        }

        public IObservable<OrderBookModifyiableEvent> Streams(IObservable<TradingPair[]> paris) =>
            new JoinMultipleStreamsInterceptor(paris, Streams);

        public void Dispose()
        {
            foreach (var partition in _partitionsReplicas)
                partition.Dispose();
        }

        public async Task<OrderBookL3Snapshot> GetOrderBookL3Snapshot(TradingPair tradingTradingPair)
        {
            ThrowIfNotHandlingTradingPair(tradingTradingPair);
            var tasks = _partitionsReplicas.Select(partition => partition.GetOrderBookL3Snapshot(tradingTradingPair));
            var firstCompleted = await Task.WhenAny(tasks);
            return firstCompleted.Result;
        }

        public async Task<OrderBookL2Snapshot> GetOrderBookL2Snapshot(TradingPair tradingTradingPair)
        {
            ThrowIfNotHandlingTradingPair(tradingTradingPair);
            var tasks = _partitionsReplicas.Select(partition => partition.GetOrderBookL2Snapshot(tradingTradingPair));
            var firstCompleted = await Task.WhenAny(tasks);
            return firstCompleted.Result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfNotHandlingTradingPair(TradingPair tradingTradingPair)
        {
            if (!_replicaTradingPairs.Contains(tradingTradingPair))
                throw new PartitionDoNotHandleTradingPair(tradingTradingPair);
        }
    }
}