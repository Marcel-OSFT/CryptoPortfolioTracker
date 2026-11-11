using System;

namespace CryptoPortfolioTracker.Models;

public class DataPoint
{
    public DateOnly Date { get; set; }
    public double ValueUsd { get; set; }
    public double ValueEur { get; set; }
    public double GetValue(string currency)
    {
        return currency switch
        {
            "usd" => ValueUsd,
            "eur" => ValueEur,
            _ => ValueUsd
        };
    }

}

public class CapitalFlowPoint
{
    public string Year { get; set; }
    public double Value { get; set; }
}
