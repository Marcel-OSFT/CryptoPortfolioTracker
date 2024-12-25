using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using LanguageExt;

namespace CryptoPortfolioTracker.Models;

public partial class Coin : BaseModel
{
    public Coin()
    {
        isAsset = false;
        note = string.Empty;
        Assets = new List<Asset>();
        PriceLevels = new List<PriceLevel>();

    }

    //*** Navigation Property
    [ObservableProperty] public ICollection<Asset> assets;
    public ICollection<PriceLevel> PriceLevels { get; set; }

    public int Id { get; set; }

    //*** Public Properties
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

    partial void OnPriceChanged(double oldValue, double newValue)
    {
        if (newValue == 0)
        {
            return;
        }

        var withinRangePerc = App._preferencesService.GetWithinRangePerc();
        var closeToPerc = App._preferencesService.GetCloseToPerc();

        foreach (var level in PriceLevels)
        {
            //var dist = Math.Abs(100 * (level.Value - newValue) / level.Value);
            var dist = (100 * (newValue - level.Value) / level.Value);
            level.DistanceToValuePerc = dist;

            if (level.Value != 0)
            {
                //var justTagged = false;

                //if ((level.Type == PriceLevelType.TakeProfit && newValue >= level.Value) ||
                //    (level.Type == PriceLevelType.Buy && newValue <= level.Value) ||
                //    (level.Type == PriceLevelType.Stop && newValue <= level.Value))
                //{
                //    level.Status = PriceLevelStatus.TaggedPrice;
                //    justTagged = true;
                //}

                //if (!justTagged)
                //{
                    // Check if the price is within the range

                    if ((level.Type == PriceLevelType.TakeProfit && newValue >= level.Value) ||
                        (level.Type == PriceLevelType.Buy && newValue <= level.Value) ||
                        (level.Type == PriceLevelType.Stop && newValue <= level.Value))
                    {
                        level.Status = PriceLevelStatus.TaggedPrice;
                        continue;  
                    // justTagged = true;
                    }

                    if (dist >= -1 * closeToPerc)
                    {
                        level.Status = PriceLevelStatus.CloseToPrice;
                        continue;
                    }
                    if (dist >= -1*withinRangePerc)
                    {
                        level.Status = PriceLevelStatus.WithinRange;
                        continue;
                    }
                    level.Status = PriceLevelStatus.NotWithinRange;
                //}
            }
           
        }
        OnPropertyChanged(nameof(PriceLevels));
    }


}
