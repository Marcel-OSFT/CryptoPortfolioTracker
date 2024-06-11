using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Runtime.CompilerServices;

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
