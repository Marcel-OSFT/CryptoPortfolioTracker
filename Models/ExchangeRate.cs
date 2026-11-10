using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Models
{
    public class ExchangeRate
    {
        [JsonPropertyName("date")]
        public DateOnly Date { get; set; }

        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }
    }
}
