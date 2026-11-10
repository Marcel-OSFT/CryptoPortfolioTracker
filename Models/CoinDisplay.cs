using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using LanguageExt;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace CryptoPortfolioTracker.Models;

public partial class CoinDisplay : BaseModel
{
    public CoinDisplay()
    {
        
    }

    public ICollection<AssetDisplay> Assets { get; set; } = new List<AssetDisplay>();
    public NarrativeDisplay Narrative { get; set; } = new NarrativeDisplay();
    [ObservableProperty] private ICollection<PriceLevelDisplay> priceLevels = new List<PriceLevelDisplay>();



    [ObservableProperty] private int id;
    [ObservableProperty] private string apiId = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string symbol = string.Empty;
    [ObservableProperty] private long rank;
    [ObservableProperty] private string imageUri = string.Empty;
    [ObservableProperty] private double price;
    [ObservableProperty] private double ath;
    [ObservableProperty] private double change52Week;
    [ObservableProperty] private double change1Month;
    [ObservableProperty] private double marketCap;
    [ObservableProperty] private string about = string.Empty;
    [ObservableProperty] private double change24Hr;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty] private bool isAsset;


}
