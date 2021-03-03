using System;
using QuoteServer.OrderBook.Primitives;

namespace QuoteServer.OrderBook.Partition.Exceptions
{
    public class PartitionDoNotHandleTradingPair : Exception
    {
        public PartitionDoNotHandleTradingPair(TradingPair tradingPair) : base(
            $"Partition is not configured to handle trading par {tradingPair}"
        )
        {
        }
    }
}