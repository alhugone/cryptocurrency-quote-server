using System;

namespace QuoteServer.OrderBook.Partition.Exceptions
{
    public class PartitionsMustBeMutuallyExclusiveByAssignedPairs : Exception
    {
        public PartitionsMustBeMutuallyExclusiveByAssignedPairs() : base(
            "Partitions' trading pairs assignment must be mutually exclusive"
        )
        {
        }
    }
}