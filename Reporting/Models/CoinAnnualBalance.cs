using System;

namespace CryptoPortfolioTracker.Reporting.Models
{
    /// <summary>
    /// Represents balances for a single coin at the start and end of a year.
    /// </summary>
    public class CoinAnnualBalance
    {
        public Guid CoinId { get; set; }

        // Ticker symbol, e.g. "BTC"
        public string Symbol { get; set; } = string.Empty;

        // Human readable coin name, e.g. "Bitcoin"
        public string Name { get; set; } = string.Empty;

        // Year these snapshots refer to (e.g. 2024)
        public int Year { get; set; }

        public decimal QuantityOnJanuary1 { get; set; }
        public decimal QuantityOnDecember31 { get; set; }

        // Balance on January 1 of the Year
        // Currency code (ISO 4217) -> balance
        public Dictionary<string, decimal> BalancesOnJanuary1 { get; set; } = new();
        
        // Balance on December 31 of the Year
        public Dictionary<string, decimal> BalancesOnDecember31 { get; set; } = new();

        // Absolute change per currency (computed)
        public IReadOnlyDictionary<string, decimal> AbsoluteChangesByCurrency => ComputePerCurrency((start, end) => end - start);
        
        // Percent change per currency (computed)
        // Rule preserved from the original code:
        // - if start == 0 and end == 0 => 0%
        // - if start == 0 and end != 0 => 100%
        // - otherwise => (end - start) / start * 100
        public IReadOnlyDictionary<string, decimal> PercentChangesByCurrency =>
            ComputePerCurrency((start, end) =>
                start == 0m
                    ? (end == 0m ? 0m : 100m)
                    : ((end - start) / start) * 100m);

        // Helper to compute a dictionary of values for the union of currency keys.
        private IReadOnlyDictionary<string, decimal> ComputePerCurrency(Func<decimal, decimal, decimal> compute)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (BalancesOnJanuary1 != null)
            {
                foreach (var k in BalancesOnJanuary1.Keys) keys.Add(k);
            }
            if (BalancesOnDecember31 != null)
            {
                foreach (var k in BalancesOnDecember31.Keys) keys.Add(k);
            }

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var currency in keys)
            {
                var start = GetBalance(BalancesOnJanuary1, currency);
                var end = GetBalance(BalancesOnDecember31, currency);
                result[currency.ToUpperInvariant()] = compute(start, end);
            }

            return result;
        }

        // Convenience lookups for single-currency callers
        private decimal GetAbsoluteChangeFor(string currencyCode) =>
            AbsoluteChangesByCurrency.TryGetValue(currencyCode, out var v) ? v : 0m;

        private decimal GetPercentChangeFor(string currencyCode) =>
            PercentChangesByCurrency.TryGetValue(currencyCode, out var v) ? v : 0m;

        // Safely read a currency balance from a dictionary with case-insensitive lookup.
        private static decimal GetBalance(Dictionary<string, decimal>? balances, string currencyCode)
        {
            if (balances is null) return 0m;

            if (balances.TryGetValue(currencyCode, out var value)) return value;

            // Fall back to case-insensitive search to be robust against inconsistent key casing.
            foreach (var kv in balances)
            {
                if (string.Equals(kv.Key, currencyCode, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            return 0m;
        }
    }
}
