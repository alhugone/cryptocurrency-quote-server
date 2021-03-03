namespace GrpcService.Configuration
{
    public class ExchangesCredentials
    {
        public CoinbaseCredentials? Coinbase { get; set; }

        public class CoinbaseCredentials
        {
            public string? ApiKey { get; set; }
            public string? UnsignedSignature { get; set; }
            public string? PassPhrase { get; set; }
            public bool IsSandbox { get; set; }
        }
    }
}