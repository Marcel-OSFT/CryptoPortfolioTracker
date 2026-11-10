
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Emit;

namespace CryptoPortfolioTracker.Models
{ 
    public partial class PriceLevelDisplay : BaseModel
    {
        public PriceLevelDisplay()
        {

        }

        [ObservableProperty] private int id;
        [ObservableProperty] private PriceLevelType type;
        [ObservableProperty] private double value;
        [ObservableProperty] private string note;
        [ObservableProperty] private PriceLevelStatus status;
        [ObservableProperty] private double distanceToValuePerc;

        [ObservableProperty] private CoinDisplay coin;

        partial void OnValueChanged(double oldValue, double newValue)
        {
            if (Coin == null || Coin.Price == 0 || newValue == 0)
            {
                DistanceToValuePerc = 0;
                return;
            }
            var dist = (100 * (Coin.Price - newValue) / Value);
            DistanceToValuePerc = dist;
        }

    }
}
