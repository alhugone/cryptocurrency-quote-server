using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace CoinbasePro.Shared.Utilities
{
    public static class JsonConfig
    {
        private static JsonSerializerSettings SerializerSettings { get; } = new()
        {
            FloatParseHandling = FloatParseHandling.Decimal,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver {NamingStrategy = new SnakeCaseNamingStrategy()},
            Error = delegate(object sender, ErrorEventArgs args)
            {
                if (args.CurrentObject == args.ErrorContext.OriginalObject)
                    Log.Error(
                        "Json serialization error {@OriginalObject} {@Member} {@ErrorMessage}",
                        args.ErrorContext.OriginalObject,
                        args.ErrorContext.Member,
                        args.ErrorContext.Error.Message
                    );
            },
        };

        public static string SerializeObject(object value) => JsonConvert.SerializeObject(value, SerializerSettings);

        internal static T DeserializeObject<T>(string contentBody) =>
            JsonConvert.DeserializeObject<T>(contentBody, SerializerSettings);
    }
}