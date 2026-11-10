using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models;

public partial class AssetTotalsDisplay : BaseModel
{
    public AssetTotalsDisplay()
    {
        
    }

    [ObservableProperty] private double qty;
    [ObservableProperty] private double costBase;
    [ObservableProperty] private double averageCostPrice;
    [ObservableProperty] private double profitLossPerc;
    [ObservableProperty] private double netProfitLossPerc;
    [ObservableProperty] private double profitLoss;
    [ObservableProperty] private double marketValue;
    [ObservableProperty] private double realizedPnL;
    [ObservableProperty] private CoinDisplay coin = new();
    [ObservableProperty] private bool isHidden = false;

    [ObservableProperty] private double netInvestment;

}
