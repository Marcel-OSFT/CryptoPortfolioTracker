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

public partial class Coin : BaseModel
{
    public Coin()
    {
        IsAsset = false;
        Note = string.Empty;
        Assets = new List<Asset>();
        PriceLevels = new List<PriceLevel>();
        Narrative = new();
    }

    //*** Navigation Property
    public ICollection<Asset> Assets { get; set; }
    public Narrative Narrative { get; set; }

    public ICollection<PriceLevel> PriceLevels { get; set; }

    public int Id { get; set; }

    //*** Public Properties
    public string ApiId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public long Rank { get; set; }
    public string ImageUri { get; set; } = string.Empty;
    public double Price { get; set; }
    public double Ath { get; set; }
    public double Change52Week { get; set; }
    public double Change1Month { get; set; }
    public double MarketCap { get; set; }
    public string About { get; set; } = string.Empty;
    public double Change24Hr { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsAsset { get; set; }


    public static Coin Empty()
        => new Coin
        {
            Id = 0,
            ApiId = string.Empty,
            Name = string.Empty,
            Symbol = string.Empty,
            Rank = 0,
            ImageUri = string.Empty,
            Price = 0,
            Ath = 0,
            Change52Week = 0,
            Change1Month = 0,
            MarketCap = 0,
            About = string.Empty,
            Change24Hr = 0,
            Note = string.Empty,
            IsAsset = false,
            Assets = new List<Asset>(),
            PriceLevels = new List<PriceLevel>(),
            Narrative = new(),
        };

    public void NotifyDerivedValuesChanged()
    {
        OnPropertyChanged(nameof(PriceLevels));
    }

    

}
