using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models;

public partial class Account : BaseModel
{
    public Account(string name = "")
    {
        Name = name;
        About = string.Empty;
        Assets = new List<Asset>();
    }

    //******* Public Properties
    [Key]
    public int Id { get; private set; }

    [ObservableProperty] private string name;
    [ObservableProperty] private string about;

    //*** Set To NotMapped in EntityTypeBuilder
    [ObservableProperty] private bool isHoldingAsset;

    //*** Set To NotMapped in EntityTypeBuilder
    [ObservableProperty] private double totalValue;

    //*** Navigation property
    [ObservableProperty] private ICollection<Asset> assets;

    public void CalculateTotalValue()
    {
        if (Assets.Count > 0)
        {
            TotalValue = Assets.Sum(x => x.Qty * x.Coin.Price);
        }
        else { TotalValue = 0; }
    }

    
}
