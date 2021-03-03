using CoinbasePro;
using CoinbasePro.Network.Authentication;

namespace TestsInfrastructureHelpers
{
    public static class AuthenticatorFactory
    {
        public static CoinbaseProClient GetCoinbaseProClient()
        {
            var config = TestConfiguration.CoinbaseCredentials;
            return new CoinbaseProClient(
                new Authenticator(config!.ApiKey, config.UnsignedSignature, config.PassPhrase),
                config.IsSandbox
            );
        }
    }
}