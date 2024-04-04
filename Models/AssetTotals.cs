using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models
{
    public partial class AssetTotals : BaseModel
    {
        
        [ObservableProperty] double qty;
        [ObservableProperty] double costBase;
        [ObservableProperty] double averageCostPrice;
        [ObservableProperty] double profitLossPerc;
        [ObservableProperty] double profitLoss;
        [ObservableProperty] double marketValue;
        [ObservableProperty] double realizedPnL;
        [ObservableProperty] Coin coin;
        [ObservableProperty] bool isHidden = false;

        // 'On...Changed'and 'On...Changing' methods provided by the 'ObservableProperty' implementation
        partial void OnQtyChanged(double value)
        {
            if (Coin != null) MarketValue = value * Coin.Price;
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
}
