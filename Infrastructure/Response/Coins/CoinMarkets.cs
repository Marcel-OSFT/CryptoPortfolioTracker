using System;
using Newtonsoft.Json;

namespace CryptoPortfolioTracker.Infrastructure.Response.Coins
{
    public class CoinMarkets
    {
        [JsonProperty("market_cap_rank")]
        public long? MarketCapRank
        {
            get; set;
        }
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
        [JsonProperty("image")]
        public Uri Image
        {
            get; set;
        }
        [JsonProperty("current_price")]
        public double? CurrentPrice
        {
            get; set;
        }
        [JsonProperty("market_cap")]
        public double? MarketCap
        {
            get; set;
        }
        [JsonProperty("ath")]
        public double? Ath
        {
            get; set;
        }

        [JsonProperty("price_change_percentage_1y_in_currency", NullValueHandling = NullValueHandling.Ignore)]
        public double? PriceChangePercentage1YInCurrency
        {
            get; set;
        }

        [JsonProperty("price_change_percentage_24h_in_currency", NullValueHandling = NullValueHandling.Ignore)]
        public double? PriceChangePercentage24HInCurrency
        {
            get; set;
        }

        [JsonProperty("price_change_percentage_30d_in_currency", NullValueHandling = NullValueHandling.Ignore)]
        public double? PriceChangePercentage30DInCurrency
        {
            get; set;
        }
    }


}
