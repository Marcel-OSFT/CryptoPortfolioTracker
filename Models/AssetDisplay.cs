using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class AssetDisplay : BaseModel
{
    public AssetDisplay()
    {
        
    }

    [ObservableProperty] private int id;
    [ObservableProperty] private double qty;
    [ObservableProperty] private double averageCostPrice;
    [ObservableProperty] private double realizedPnL;
    [ObservableProperty] private CoinDisplay coin;
    [ObservableProperty] private AccountDisplay account;
    [ObservableProperty] public ICollection<MutationDisplay> mutations;

    
}
 