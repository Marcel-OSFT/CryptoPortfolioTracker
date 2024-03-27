using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CryptoPortfolioTracker.Infrastructure.Response.Coins
{
    public class CoinFullDataById
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("symbol")] public string Symbol { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("image")] public Image Image { get; set; }
        [JsonProperty("description")] public Dictionary<string, string> Description { get; set; }
        [JsonProperty("market_cap_rank")] public long? MarketCapRank { get; set; }
        [JsonProperty("market_data", NullValueHandling = NullValueHandling.Ignore)] public MarketData MarketData { get; set; }
    }

    public class MarketData 
    {
        [JsonProperty("market_cap_rank")] public long? MarketCapRank { get; set; }
        [JsonProperty("ath")] public Dictionary<string, double?> Ath { get; set; }
        [JsonProperty("current_price")] public Dictionary<string, double?> CurrentPrice { get; set; }
        [JsonProperty("market_cap")] public Dictionary<string, double?> MarketCap { get; set; }
        [JsonProperty("price_change_percentage_24h_in_currency")] public Dictionary<string, double> PriceChangePercentage24HInCurrency { get; set; }
        [JsonProperty("price_change_percentage_30d_in_currency")] public Dictionary<string, double> PriceChangePercentage30DInCurrency { get; set; }
        [JsonProperty("price_change_percentage_1y_in_currency")] public Dictionary<string, double> PriceChangePercentage1YInCurrency { get; set; }
    }

    public class Image
    {
        [JsonProperty("thumb")] public Uri Thumb { get; set; }
        [JsonProperty("small")] public Uri Small { get; set; }
        [JsonProperty("large")] public Uri Large { get; set; }
    }

}