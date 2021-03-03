using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Binance.API.Csharp.Client;
using Binance.API.Csharp.Client.Models.WebSocket;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Binance.QuoteSourceTests
{
    public class UnitTest1
    {
        private ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Test1()
        {
            Thread.CurrentThread.CurrentCulture=CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture=CultureInfo.InvariantCulture;
            var apiClient = new ApiClient(
                "idiRu6vAM8CjShqQQsbPwM0PQSeS4YwVcXugnaYbp9skfPg0uOGA28w9ZwUatnUW",
                "h0RKpQqWh5IMrMkFljAFV5KdGMwiYnyOk5SVfiZYHq7eebVxO8IE5v9wIGSLl2K9"
            );
            var binanceClient = new BinanceClient(apiClient);
            var result= await binanceClient.GetOrder("ethbtc");
            _output.WriteLine(JsonConvert.SerializeObject(result,Formatting.Indented));
            
            binanceClient.ListenTradeEndpoint("ethbtc", AggregateTradesHandler);
            Thread.Sleep(50000);
        }

        private void AggregateTradesHandler(AggregateTradeMessage messagedata)
        {
            messagedata.
        }
    }
}