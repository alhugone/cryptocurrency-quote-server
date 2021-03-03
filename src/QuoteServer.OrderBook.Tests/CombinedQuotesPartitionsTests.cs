using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using QuoteServer.OrderBook.Partition;
using QuoteServer.OrderBook.Partition.Exceptions;
using QuoteServer.OrderBook.Partition.Model;
using QuoteServer.OrderBook.Primitives;
using Xunit;

namespace QuoteServer.OrderBook.Tests
{
    public class CombinedQuotesPartitionsTests
    {
        [Fact]
        public void WhenPartitionAssignmentOverlapsByPair_ThrowsException()
        {
            // arrange
            var mock = new Mock<IQuotesPartition>();
            var list = new List<(IQuotesPartition partition, ISet<TradingPair> assignedPairs)>
            {
                (mock.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveBtc})),
                (mock.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveEur, TradingPair.AaveGbp})),
                (mock.Object, new HashSet<TradingPair>(new[] {TradingPair.EthGbp, TradingPair.AaveGbp})),
            };
            // act & assert
            Assert.Throws<PartitionsMustBeMutuallyExclusiveByAssignedPairs>(() => new CombinedQuotesPartitions(list));
        }

        [Fact]
        public async Task WhenCallingMethodWithPairThatIsNotHandled_ThrowsException()
        {
            // arrange
            var mock = new Mock<IQuotesPartition>();
            var list = new List<(IQuotesPartition partition, ISet<TradingPair> assignedPairs)>
            {
                (mock.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveBtc})),
                (mock.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveEur, TradingPair.AaveGbp})),
            };
            var cut = new CombinedQuotesPartitions(list);
            // act & assert
            Assert.Throws<PartitionDoNotHandleTradingPair>(() => cut.Streams(TradingPair.Unknown));
            await Assert.ThrowsAsync<PartitionDoNotHandleTradingPair>(
                () => cut.GetOrderBookL2Snapshot(TradingPair.Unknown)
            );
            await Assert.ThrowsAsync<PartitionDoNotHandleTradingPair>(
                () => cut.GetOrderBookL3Snapshot(TradingPair.Unknown)
            );
        }

        [Fact]
        public async Task WhenCallingMethodWithPairThatIsHandled_ThenForwardCallToAssignedToThatPairPartition()
        {
            // arrange
            var expectedpairs = (TradingPair.AaveBtc, TradingPair.AaveEur);
            var (mock1, mock2) = (new Mock<IQuotesPartition>(), new Mock<IQuotesPartition>());
            var list = new List<(IQuotesPartition partition, ISet<TradingPair> assignedPairs)>
            {
                (mock1.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveBtc})),
                (mock2.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveEur, TradingPair.AaveGbp})),
            };
            var cut = new CombinedQuotesPartitions(list);
            // act
            await CallAllMethods(expectedpairs.AaveBtc);
            await CallAllMethods(expectedpairs.AaveEur);
            // assert
            AssertMethodsHasBeenCalled(mock1, expectedpairs.AaveBtc);
            AssertMethodsHasBeenCalled(mock2, expectedpairs.AaveEur);

            async Task CallAllMethods(TradingPair pair)
            {
                cut.Streams(pair);
                await cut.GetOrderBookL2Snapshot(pair);
                await cut.GetOrderBookL3Snapshot(pair);
            }

            void AssertMethodsHasBeenCalled(Mock<IQuotesPartition> mock, TradingPair expectedPair)
            {
                mock.Verify(x => x.Streams(expectedPair), Times.Once);
                mock.Verify(x => x.GetOrderBookL2Snapshot(expectedPair), Times.Once);
                mock.Verify(x => x.GetOrderBookL3Snapshot(expectedPair), Times.Once);
            }
        }

        [Fact]
        public void ForwardsDisposeToPartitions()
        {
            // arrange
            var (mock1, mock2) = (new Mock<IQuotesPartition>(), new Mock<IQuotesPartition>());
            var list = new List<(IQuotesPartition partition, ISet<TradingPair> assignedPairs)>
            {
                (mock1.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveBtc})),
                (mock2.Object, new HashSet<TradingPair>(new[] {TradingPair.AaveEur, TradingPair.AaveGbp})),
            };
            var cut = new CombinedQuotesPartitions(list);
            // act
            cut.Dispose();
            // assert
            mock1.Verify(x => x.Dispose(), Times.Once);
            mock2.Verify(x => x.Dispose(), Times.Once);
        }
    }
}