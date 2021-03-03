using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using QuoteServer.OrderBook.Partition.Events;

namespace QuoteServer.OrderBook.Partition.Readers
{
    internal class DefaultReaderFactory : IReadersFactory
    {
        public void StartNew(
            int readerId,
            ChannelReader<OrderBookModifyiableEvent> channel,
            List<QuotesPartition.TradingPairSubscription>[] activeSubscriptionsToPairs,
            CancellationToken cancellationToken)
        {
            Task.Run(
                () => ReadAndBroadcastEvents(channel, activeSubscriptionsToPairs, cancellationToken),
                cancellationToken
            );
        }

        private static void ReadAndBroadcastEvents(
            ChannelReader<OrderBookModifyiableEvent> readerChannel,
            IReadOnlyList<List<QuotesPartition.TradingPairSubscription>> activeSubscriptionsToTradingPairs,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                OrderBookModifyiableEvent? msg = null;
                while (!readerChannel.TryRead(out msg))
                    if (cancellationToken.IsCancellationRequested)
                        return;
                var tradingPairSubscriptions = activeSubscriptionsToTradingPairs[(int) msg.TradingPair];
                for (var i = 0; i < tradingPairSubscriptions.Count; i++)
                    tradingPairSubscriptions[i].Observer?.OnNext(msg);
            }
            ;
        }
    }
}