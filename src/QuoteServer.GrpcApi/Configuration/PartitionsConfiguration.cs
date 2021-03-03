using System.Collections.Generic;

namespace GrpcService.Configuration
{
    public class PartitionsConfiguration
    {
        public IReadOnlyList<CoinbasePartitionConfiguration>? Partitions { get; set; }

        public class CoinbasePartitionConfiguration
        {
            public List<string> OnlyPairs { get; set; } = new();
            public List<string> AllExcept { get; set; } = new();
        }
    }
}