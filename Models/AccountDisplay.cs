using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models;

public partial class AccountDisplay : BaseModel
{
    public AccountDisplay()
    {

    }

    [ObservableProperty] private int id;
    [ObservableProperty] private string name;
    [ObservableProperty] private string about;
    [ObservableProperty] private bool isHoldingAsset;
    [ObservableProperty] private double totalValue;
    [ObservableProperty] private ICollection<AssetDisplay> assets;


    
}
