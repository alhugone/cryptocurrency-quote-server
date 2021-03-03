using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using QuoteServer.OrderBook.Partition.Events;

namespace QuoteServer.OrderBook.Partition.Readers
{
    public interface IReadersFactory
    {
        void StartNew(
            int readerId,
            ChannelReader<OrderBookModifyiableEvent> channel,
            List<QuotesPartition.TradingPairSubscription>[] activeSubscriptionsToPairs,
            CancellationToken cancellationToken);
    }
}