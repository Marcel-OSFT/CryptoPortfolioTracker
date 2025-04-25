
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Emit;

namespace CryptoPortfolioTracker.Models
{ 
    public class PriceLevelBuilder
    {
        private int _id;
        private PriceLevelType _type;
        private double _value;
        private string _note = string.Empty;
        private PriceLevelStatus _status;
        private double _distanceToValuePerc;
        private Coin _coin;

        private PriceLevelBuilder()
        {
            
        }
        public static PriceLevelBuilder Create() => new PriceLevelBuilder();

        public PriceLevelBuilder OfType(PriceLevelType type)
        {
            _type = type;
            return this;
        }
        public PriceLevelBuilder WithValue(double value)
        {
            _value = value;
            return this;
        }
        public PriceLevelBuilder WithNote(string note)
        {
            _note = note;
            return this;
        }
        
        public PriceLevelBuilder ForCoin(Coin coin)
        {
            _coin = coin;
            return this;
        }
        public PriceLevel Build()
        {
            //_coin.EvaluatePriceLevels(_coin.Price);

            return new PriceLevel
            {
                Type = _type,
                Value = _value,
                Note = _note,
                Coin = _coin
            };
        }
    }


}
