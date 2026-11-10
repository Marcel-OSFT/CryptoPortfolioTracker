using System;

namespace CryptoPortfolioTracker.Models;

public sealed class DailyExchangeRate
{
    public DailyExchangeRate(DateOnly date, double rate)
    {
        Date = date;
        Rate = rate;
    }

    public DateOnly Date { get; }
    public double Rate { get; }
}
