using System;

namespace CryptoPortfolioTracker.Reporting.Models
{
    /// <summary>
    /// Report container for the yearly coin balances.
    /// </summary>
    public class AnnualCoinBalancesReport
    {
        // Year covered by the report
        public int Year { get; set; }

        // Rows in the report
        public IReadOnlyList<CoinAnnualBalance> Items { get; set; } = Array.Empty<CoinAnnualBalance>();
    }
}