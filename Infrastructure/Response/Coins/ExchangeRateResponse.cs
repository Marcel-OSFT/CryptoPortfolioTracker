using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Infrastructure.Response.Coins
{
    public class CoinGeckoExchangeRatesResponse
    {
        [JsonProperty("rates")]
        public Dictionary<string, CoinGeckoRate> Rates { get; set; } = new();
    }

    public class CoinGeckoRate
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("unit")]
        public string Unit { get; set; } = string.Empty;

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;
    }
}