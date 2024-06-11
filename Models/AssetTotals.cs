using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptoPortfolioTracker.Models;

public partial class AssetTotals : BaseModel
{
    public AssetTotals()
    {
        
    }

    [ObservableProperty] private double qty;
    [ObservableProperty] private double costBase;
    [ObservableProperty] private double averageCostPrice;
    [ObservableProperty] private double profitLossPerc;
    [ObservableProperty] private double profitLoss;
    [ObservableProperty] private double marketValue;
    [ObservableProperty] private double realizedPnL;
    [ObservableProperty] private Coin coin = new();
    [ObservableProperty] private bool isHidden = false;

    // 'On...Changed'and 'On...Changing' methods provided by the 'ObservableProperty' implementation
    partial void OnQtyChanged(double value)
    {
        if (Coin != null && Coin.Name != string.Empty) MarketValue = value * Coin.Price;
        AverageCostPrice = CostBase / value;
    }

    partial void OnCostBaseChanged(double value)
    {
        AverageCostPrice = value / Qty;
        ProfitLoss = MarketValue - value;
        ProfitLossPerc = 100 * ((MarketValue - value) / value);
    }
    partial void OnMarketValueChanged(double value)
    {
        ProfitLoss = value - CostBase;
        ProfitLossPerc = 100 * ((value - CostBase) / CostBase);
    }
    partial void OnCoinChanged(Coin value)
    {
        MarketValue = Qty * value.Price;
    }

    
}
