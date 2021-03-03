using GrpcService;
using GrpcService.Configuration;
using Microsoft.Extensions.Configuration;

namespace TestsInfrastructureHelpers
{
    public class TestConfiguration
    {
        static TestConfiguration()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddUserSecrets<Program>().Build();
            CoinbaseCredentials = config.GetSection(nameof(ExchangesCredentials)).Get<ExchangesCredentials>().Coinbase;
        }

        public static ExchangesCredentials.CoinbaseCredentials? CoinbaseCredentials { get; }
    }
}