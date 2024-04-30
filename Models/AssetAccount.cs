using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models;


public partial class AssetAccount : BaseModel
{
    public AssetAccount()
    {
        name = string.Empty;
        symbol = string.Empty;
    }


    [ObservableProperty] private string name;

    [ObservableProperty] private double qty;

    [ObservableProperty] private string symbol;

    [ObservableProperty] private int assetId;

}
