
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
        [ObservableProperty] [NotMapped] private double distanceToValuePerc;

        //******* Navigation Properties
        [ObservableProperty] private Coin coin;

        partial void OnValueChanged(double oldValue, double newValue)
        {
            //var dist = Math.Abs(100 * (coin.Price - newValue) / Value);
            var dist = (100 * (Coin.Price - newValue) / Value);
            DistanceToValuePerc = dist; 
        }
    }

}
