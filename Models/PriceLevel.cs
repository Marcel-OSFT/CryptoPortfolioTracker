
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using System;
using System.Collections.Generic;

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

        //******* Navigation Properties
        [ObservableProperty] private Coin coin;
        
    }






}
