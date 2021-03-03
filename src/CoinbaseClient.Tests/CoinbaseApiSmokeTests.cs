using System.Threading.Tasks;
using FluentAssertions;
using TestsInfrastructureHelpers;
using Xunit;

namespace CoinbaseClient.Tests
{
    public class CoinbaseApiSmokeTests
    {
        [Fact]
        public async Task WithGivenCredentialsCanConnectToCoinbase()
        {
            var coinbaseProClient = AuthenticatorFactory.GetCoinbaseProClient();
            var allAccounts = await coinbaseProClient.AccountsService.GetAllAccountsAsync();
            allAccounts.Should().NotBeEmpty();
        }
    }
}