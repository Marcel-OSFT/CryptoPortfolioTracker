using Newtonsoft.Json;

namespace CryptoPortfolioTracker.Infrastructure.Response.Coins;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class CoinList
{
    [JsonProperty("id")]
    public string Id
    {
        get; set;
    }
    [JsonProperty("symbol")]
    public string Symbol
    {
        get; set;
    }
    [JsonProperty("name")]
    public string Name
    {
        get; set;
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
