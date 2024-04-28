using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models;

public partial class Coin : BaseModel
{
    public Coin()
    {
        isAsset = false;
        note = string.Empty;
    }

    //*** Navigation Property
    public ICollection<Asset> Assets
    {
        get; set;
    }
    public int Id
    {
        get; set;
    }

    //*** Public Properties
    [ObservableProperty] string apiId;
    [ObservableProperty] string name;
    [ObservableProperty] string symbol;
    [ObservableProperty] long rank;
    [ObservableProperty] string imageUri = string.Empty;
    [ObservableProperty] double price;
    [ObservableProperty] double ath;
    [ObservableProperty] double change52Week;
    [ObservableProperty] double change1Month;
    [ObservableProperty] double marketCap;
    [ObservableProperty] string about = string.Empty;
    [ObservableProperty] double change24Hr;
    [ObservableProperty] string note = string.Empty;
    [ObservableProperty] bool isAsset;


}
