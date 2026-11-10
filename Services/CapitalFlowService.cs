using System;
using System.Collections.Generic;

namespace CryptoPortfolioTracker.Services;

public interface ICapitalFlowService
{
    CapitalFlowData CapitalFlow { get; set; }
    double CapitalFlowExchangeRateEurUsd { get; }

    double GetInFlowForCurrency();
    double GetNetFlowForCurrency();
    double GetOutFlowForCurrency();
    List<YearlyFlow> GetYearlyInFlowForCurrency();
    List<YearlyFlow> GetYearlyOutFlowForCurrency();
}

public sealed class CapitalFlowService : ICapitalFlowService
{
    private readonly Settings _settings;

    public CapitalFlowData CapitalFlow { get; set; } = new();

    public double CapitalFlowExchangeRateEurUsd => GetCapitalFlowExchangeRate();

    public CapitalFlowService(Settings settings)
    {
        _settings = settings;
    }

    public double GetNetFlowForCurrency()
    {
        return _settings.Currency.Equals("eur", StringComparison.OrdinalIgnoreCase)
            ? CapitalFlow.NetFlowEur
            : CapitalFlow.NetFlowUsd;
    }

    public List<YearlyFlow> GetYearlyInFlowForCurrency()
    {
        return CapitalFlow.GetYearlyInFlowForCurrency(_settings.Currency);
    }

    public List<YearlyFlow> GetYearlyOutFlowForCurrency()
    {
        return CapitalFlow.GetYearlyOutFlowForCurrency(_settings.Currency);
    }

    public double GetInFlowForCurrency()
    {
        return CapitalFlow.GetInFlowForCurrency(_settings.Currency);
    }
    public double GetOutFlowForCurrency()
    {
        return CapitalFlow.GetOutFlowForCurrency(_settings.Currency);
    }

    public double GetCapitalFlowExchangeRate()
    {
        var eurNet = CapitalFlow.NetFlowEur;
        if (Math.Abs(eurNet) < double.Epsilon)
        {
            return 0;
        }

        return CapitalFlow.NetFlowUsd / eurNet;
    }
}
