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

namespace CryptoPortfolioTracker.Models;

public class CoinBuilder
{
    private CoinBuilder()
    {
       
    }

    private ICollection<Asset> _assets = new List<Asset>();
    private Narrative _narrative = new();

    private ICollection<PriceLevel> _priceLevels = new List<PriceLevel>();

    private string _apiId = string.Empty;
    private string _name = string.Empty;
    private string _symbol = string.Empty;
    private long _rank;
    private string _imageUri = string.Empty;
    private double _price;
    private double _ath;
    private double _change52Week;
    private double _change1Month;
    private double _marketCap;
    private string _about = string.Empty;
    private double _change24Hr;
    private string _note = string.Empty;
    private bool _isAsset = false;


    public static CoinBuilder Create() => new CoinBuilder();

    public CoinBuilder WithApiId(string apiId)
    {
        _apiId = apiId;
        return this;
    }
    public CoinBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    public CoinBuilder WithSymbol(string symbol)
    {
        _symbol = symbol;
        return this;
    }
    public CoinBuilder MarketCapRankAt(long rank)
    {
        _rank = rank;
        return this;
    }
    public CoinBuilder WithImage(string imageUri)
    {
        _imageUri = imageUri;
        return this;
    }
    public CoinBuilder CurrentPriceAt(double price)
    {
        _price = price;
        return this;
    }
    public CoinBuilder AllTimeHighAt(double ath)
    {
        _ath = ath;
        return this;
    }
    public CoinBuilder YearlyChangeAt(double change52Week)
    {
        _change52Week = change52Week;
        return this;
    }
    public CoinBuilder MonthlyChangeAt(double change1Month)
    {
        _change1Month = change1Month;
        return this;
    }
    public CoinBuilder MarketCapAt(double marketCap)
    {
        _marketCap = marketCap;
        return this;
    }
    public CoinBuilder WithAbout(string about)
    {
        _about = about;
        return this;
    }
    public CoinBuilder DailyChangeAt(double change24Hr)
    {
        _change24Hr = change24Hr;
        return this;
    }
    public CoinBuilder WithNote(string note)
    {
        _note = note;
        return this;
    }
    public CoinBuilder MarkedAsAsset(bool isAsset)
    {
        _isAsset = isAsset;
        return this;
    }
    public CoinBuilder OfAsset(Asset asset)
    {
        _assets.Add(asset);
        return this;
    }
    public CoinBuilder OfNarrative(Narrative narrative)
    {
        _narrative = narrative;
        return this;
    }
    
    public CoinBuilder WithPriceLevel(Action<PriceLevelBuilder> options)
    {
        var priceLevel = PriceLevelBuilder.Create();
        options(priceLevel);
        _priceLevels.Add(priceLevel.Build());
        return this;
    }


    public Coin Build()
    {
        return new Coin
        {
            ApiId = _apiId,
            Name = _name,
            Symbol = _symbol,
            Rank = _rank,
            ImageUri = _imageUri,
            Price = _price,
            Ath = _ath,
            Change52Week = _change52Week,
            Change1Month = _change1Month,
            MarketCap = _marketCap,
            About = _about,
            Change24Hr = _change24Hr,
            Note = _note,
            IsAsset = _isAsset,
            Assets = _assets,
            Narrative = _narrative,
            PriceLevels = _priceLevels
        };

    }
}
