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
        isAsset = false;
        note = string.Empty;
        Assets = new List<Asset>();
        PriceLevels = new List<PriceLevel>();
        Narrative = new();
    }

    //*** Navigation Property
    [ObservableProperty] public ICollection<Asset> assets;
    [ObservableProperty] public Narrative narrative;

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

    [NotMapped] public double Rsi { get; set; }
    [NotMapped] public double Ema { get; set; }
    [NotMapped] private List<double> ClosingPrices { get; set; } = new();
    [NotMapped] private DateTime FileDateMarketChart { get; set; } = DateTime.MinValue;

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

    async partial void OnPriceChanged(double oldValue, double newValue)
    {
        //await CalculateRsi();
        //await CalculateMa();
        
        //EvaluatePriceLevels(newValue);
    }

    public void NotifyDerivedValuesChanged()
    {
        OnPropertyChanged(nameof(Rsi));
        OnPropertyChanged(nameof(Ema));
        OnPropertyChanged(nameof(PriceLevels));
    }

    //private void UpdatePriceLevelEma()
    //{
    //    var priceLevelEma = PriceLevels.FirstOrDefault(p => p.Type == PriceLevelType.Ema);
    //    if (priceLevelEma != null)
    //    {
    //        priceLevelEma.Value = Ema;
    //        priceLevelEma.DistanceToValuePerc = (100 * (Price - Ema) / Ema);
    //    }
    //    else
    //    {
    //        var newPriceLevel = new PriceLevel
    //        {
    //            Type = PriceLevelType.Ema,
    //            Value = Ema,
    //            Coin = this,
    //            DistanceToValuePerc = (100 * (Price - Ema) / Ema)
    //        };
    //        PriceLevels.Add(newPriceLevel);
    //    }
    //}

    //public void EvaluatePriceLevels(double newValue)
    //{
    //    if (newValue == 0)
    //    {
    //        return;
    //    }

    //    var withinRangePerc = Settings.WithinRangePerc;
    //    var closeToPerc = Settings.CloseToPerc;

    //    foreach (var level in PriceLevels)
    //    {
    //        var dist = (100 * (newValue - level.Value) / level.Value);
    //        level.DistanceToValuePerc = dist;

    //        if (level.Value != 0)
    //        {
    //            if ((level.Type == PriceLevelType.TakeProfit && newValue >= level.Value) ||
    //                (level.Type == PriceLevelType.Buy && newValue <= level.Value) ||
    //                (level.Type == PriceLevelType.Stop && newValue <= level.Value) ||
    //                (level.Type == PriceLevelType.Ema && newValue <= level.Value))
    //            {
    //                level.Status = PriceLevelStatus.TaggedPrice;
    //                continue;
    //            }

    //            if (dist >= -1 * closeToPerc)
    //            {
    //                level.Status = PriceLevelStatus.CloseToPrice;
    //                continue;
    //            }
    //            if (dist >= -1 * withinRangePerc)
    //            {
    //                level.Status = PriceLevelStatus.WithinRange;
    //                continue;
    //            }
    //            level.Status = PriceLevelStatus.NotWithinRange;
    //        }
    //    }
    //    OnPropertyChanged(nameof(PriceLevels));
    //}

    //public async Task CalculateRsi()
    //{
    //    try
    //    {
    //        await GetClosingPrices();

    //        var prices = ClosingPrices.ToList();
    //        prices.Add(Price);

    //        //*** RSI calculation
    //        int period = Settings.RsiPeriod;

    //        if (period == 0 || prices == null || prices.Count < period + 1)
    //        {
    //            Rsi = 0;
    //            return;
    //        }

    //        List<double> rsiValues = new List<double>();
    //        List<double> gains = new List<double>();
    //        List<double> losses = new List<double>();

    //        for (int i = 1; i <= period; i++)
    //        {
    //            double change = prices[i] - prices[i - 1];
    //            if (change > 0)
    //            {
    //                gains.Add(change);
    //                losses.Add(0);
    //            }
    //            else
    //            {
    //                gains.Add(0);
    //                losses.Add(-change);
    //            }
    //        }

    //        double avgGain = gains.Average();
    //        double avgLoss = losses.Average();

    //        double alpha = 1.0 / period;

    //        double rs = avgGain / avgLoss;
    //        double rsi = 100 - (100 / (1 + rs));
    //        rsiValues.Add(rsi);

    //        for (int i = period + 1; i < prices.Count; i++)
    //        {
    //            double change = prices[i] - prices[i - 1];
    //            double gain = change > 0 ? change : 0;
    //            double loss = change < 0 ? -change : 0;

    //            avgGain = (gain * alpha) + (avgGain * (1 - alpha));
    //            avgLoss = (loss * alpha) + (avgLoss * (1 - alpha));

    //            rs = avgGain / avgLoss;
    //            rsi = 100 - (100 / (1 + rs));
    //            rsiValues.Add(rsi);
    //        }

    //        Rsi = rsiValues.Last();
    //        return;
    //    }
    //    catch (Exception ex)
    //    {
    //        Rsi = 0;
    //        return;
    //    }
    //}

    //private async Task GetClosingPrices()
    //{
    //    var fileName = Path.Combine(AppConstants.ChartsFolder, $"MarketChart_" + ApiId + ".json");

    //    if (!File.Exists(fileName))
    //    {
    //        ClosingPrices = new List<double>();
    //        return;
    //    }

    //    FileInfo fi = new(fileName);
    //    DateTime fileDate = fi.LastWriteTime;

    //    if (FileDateMarketChart != fileDate || !ClosingPrices.Any())
    //    {
    //        MarketChartById marketChart = new();

    //        var suffix = Name.Contains("_pre-listing") ? "-prelisting" : "";

    //        await marketChart.LoadMarketChartJson(ApiId + suffix);

    //        if (marketChart.Prices.Length > 0)
    //        {
    //            ClosingPrices = marketChart.Prices.TakeLast(150).Select(p => (double)p[1].Value).ToList();
    //            FileDateMarketChart = fileDate;
    //        }
    //    }
    //}


    //public async Task<double> CalculateMa()
    //{
    //    await GetClosingPrices();

    //    var prices = ClosingPrices.ToList();
    //    prices.Add(Price);

    //    //*** MA calculation
    //    int period = Settings.MaPeriod;
    //    string maType = Settings.MaType; // "EMA" or "SMA"

    //    if (period == 0 || prices == null || prices.Count < period)
    //    {
    //        Ema = 0;
    //        return 0;
    //    }

    //    // If SMA requested, return the simple average of the most recent 'period' prices
    //    if (string.Equals(maType, "SMA", StringComparison.OrdinalIgnoreCase))
    //    {
    //        double smaRecent = prices.TakeLast(period).Average();
    //        Ema = smaRecent;
    //        UpdatePriceLevelEma();
    //        return smaRecent;
    //    }

    //    // Default: EMA calculation
    //    // Calculate the initial SMA (Simple Moving Average) for the first 'period' values
    //    double sma = prices.Take(period).Average();

    //    // Multiplier for weighting the EMA
    //    double multiplier = 2.0 / (period + 1);

    //    // Start EMA with the SMA
    //    double ema = sma;

    //    // Calculate EMA for the rest of the prices (if any)
    //    for (int i = period; i < prices.Count; i++)
    //    {
    //        ema = ((prices[i] - ema) * multiplier) + ema;
    //    }

    //    Ema = ema;
    //    UpdatePriceLevelEma();
    //    return ema;
    //}


}
