using Coinbase.QuoteSource;
using QuoteServer.OrderBook.Partition;
using TestsInfrastructureHelpers;

namespace CoinbaseClient.Tests
{
    internal static class CoinbaseQuoteSourceFactory
    {
        public static QuotesPartition GetCoinbaseQuoteSource()
        {
            var coinbaseConnection = new CoinbaseQuotesSourceConnection(AuthenticatorFactory.GetCoinbaseProClient());
            return new QuotesPartition(coinbaseConnection);
        }
    }
}