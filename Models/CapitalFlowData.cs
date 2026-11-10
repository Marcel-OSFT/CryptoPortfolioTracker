using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoPortfolioTracker.Models;

public sealed class YearlyFlow
{
    public int Year { get; set; }
    public double Value { get; set; }
}

public sealed class CapitalFlowData
{
    public List<YearlyFlow> InFlowUsdYearly { get; set; } = new();
    public List<YearlyFlow> OutFlowUsdYearly { get; set; } = new();
    public List<YearlyFlow> InFlowEurYearly { get; set; } = new();
    public List<YearlyFlow> OutFlowEurYearly { get; set; } = new();

    public double InFlowUsdTotal => InFlowUsdYearly.Sum(x => x.Value);
    public double OutFlowUsdTotal => OutFlowUsdYearly.Sum(x => x.Value);
    public double InFlowEurTotal => InFlowEurYearly.Sum(x => x.Value);
    public double OutFlowEurTotal => OutFlowEurYearly.Sum(x => x.Value);

    public double NetFlowUsd => InFlowUsdTotal - OutFlowUsdTotal;
    public double NetFlowEur => InFlowEurTotal - OutFlowEurTotal;

    public List<YearlyFlow> GetYearlyInFlowForCurrency(string currency)
    {
        return currency.Equals("eur", StringComparison.OrdinalIgnoreCase)
            ? InFlowEurYearly
            : InFlowUsdYearly;
    }

    public List<YearlyFlow> GetYearlyOutFlowForCurrency(string currency)
    {
        return currency.Equals("eur", StringComparison.OrdinalIgnoreCase)
            ? OutFlowEurYearly
            : OutFlowUsdYearly;
    }

    public double GetInFlowForCurrency(string currency)
    {
        return currency.Equals("eur", StringComparison.OrdinalIgnoreCase)
            ? InFlowEurTotal
            : InFlowUsdTotal;
    }
    public double GetOutFlowForCurrency(string currency)
    {
        return currency.Equals("eur", StringComparison.OrdinalIgnoreCase)
            ? OutFlowEurTotal
            : OutFlowUsdTotal;
    }





}
