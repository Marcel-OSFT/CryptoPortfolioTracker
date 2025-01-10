
using System;

namespace CryptoPortfolioTracker.Models
{
    public class Portfolio
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        public DateTime LastAccess { get; set; } = DateTime.MinValue;

    }
}
