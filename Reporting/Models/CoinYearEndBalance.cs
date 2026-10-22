using System;

namespace CryptoPortfolioTracker.Reporting.Models

{
    /// <summary>
    /// Represents balances for a single coin at the start and end of a year.
    /// </summary>
    public class CoinYearEndBalance
    {
        public Guid CoinId { get; set; }

        // Year these snapshots refer to (e.g. 2024)
        public int Year { get; set; }
        // Snapshot date is implied to be Dec 31 of <see cref="Year" />
        public DateTime SnapshotDate => new DateTime(Year, 12, 31);
        public decimal QuantityOnDecember31 { get; set; }

        // Balance on December 31 of the Year
        // Currency code (ISO 4217) -> balance
        public Dictionary<string, decimal> Balances { get; set; } = new();

    }
}