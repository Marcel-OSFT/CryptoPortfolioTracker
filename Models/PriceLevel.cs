
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Emit;

namespace CryptoPortfolioTracker.Models
{ 
    public partial class PriceLevel : BaseModel
    {
        public PriceLevel()
        {
            Coin = new Coin();
            note = string.Empty;
        }

        [ObservableProperty] private int id;
        [ObservableProperty] private PriceLevelType type;
        [ObservableProperty] private double value;
        [ObservableProperty] private string note;
        [ObservableProperty] private PriceLevelStatus status;
       // [ObservableProperty] [NotMapped] public double distanceToValuePerc;
        [NotMapped] public double DistanceToValuePerc { get; set; }

        //******* Navigation Properties
        [ObservableProperty] private Coin coin;

        partial void OnValueChanged(double oldValue, double newValue)
        {
            var dist = (100 * (Coin.Price - newValue) / Value);
            DistanceToValuePerc = dist;
        }

    }
}
