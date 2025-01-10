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
    [NotMapped] private List<double> ClosingPrices { get; set; } = new();
    [NotMapped] private DateTime FileDateMarketChart { get; set; } = DateTime.MinValue;


    partial void OnPriceChanged(double oldValue, double newValue)
    {
        EvaluatePriceLevels(newValue);
        CalculateRsiAsync();
    }

    private void EvaluatePriceLevels(double newValue)
    {
        if (newValue == 0)
        {
            return;
        }

        var withinRangePerc = App._preferencesService.GetWithinRangePerc();
        var closeToPerc = App._preferencesService.GetCloseToPerc();

        foreach (var level in PriceLevels)
        {
            var dist = (100 * (newValue - level.Value) / level.Value);
            level.DistanceToValuePerc = dist;

            if (level.Value != 0)
            {
                if ((level.Type == PriceLevelType.TakeProfit && newValue >= level.Value) ||
                    (level.Type == PriceLevelType.Buy && newValue <= level.Value) ||
                    (level.Type == PriceLevelType.Stop && newValue <= level.Value))
                {
                    level.Status = PriceLevelStatus.TaggedPrice;
                    continue;
                }

                if (dist >= -1 * closeToPerc)
                {
                    level.Status = PriceLevelStatus.CloseToPrice;
                    continue;
                }
                if (dist >= -1 * withinRangePerc)
                {
                    level.Status = PriceLevelStatus.WithinRange;
                    continue;
                }
                level.Status = PriceLevelStatus.NotWithinRange;
            }
        }
        OnPropertyChanged(nameof(PriceLevels));
    }

    public async void CalculateRsiAsync()
    {
        Rsi = await CalculateRsi();
    }


    public async Task<double> CalculateRsi()
    {
        int period = 14;
        try
        {
            var fileName = Path.Combine(App.appDataPath, App.ChartsFolder, $"MarketChart_" + ApiId + ".json");

            if (!File.Exists(fileName))
            {
                Debug.WriteLine($"file for {ApiId} does not exist");
                return 0;
            }

            FileInfo fi = new(fileName);
            DateTime fileDate = fi.LastWriteTime;

            if (FileDateMarketChart != fileDate || !ClosingPrices.Any())
            {
                MarketChartById marketChart = new();
                await marketChart.LoadMarketChartJson(apiId);
                ClosingPrices = marketChart.Prices.TakeLast(2 * period + 50).Select(p => (double)p[1].Value).ToList();
                FileDateMarketChart = fileDate;
            }

            var prices = ClosingPrices.ToList();
            prices.Add(Price);

            if (prices == null || prices.Count < period + 1)
                throw new ArgumentException("Insufficient data to calculate RSI");

            List<double> rsiValues = new List<double>();
            List<double> gains = new List<double>();
            List<double> losses = new List<double>();

            for (int i = 1; i <= period; i++)
            {
                double change = prices[i] - prices[i - 1];
                if (change > 0)
                {
                    gains.Add(change);
                    losses.Add(0);
                }
                else
                {
                    gains.Add(0);
                    losses.Add(-change);
                }
            }

            double avgGain = gains.Average();
            double avgLoss = losses.Average();

            double alpha = 1.0 / period;

            double rs = avgGain / avgLoss;
            double rsi = 100 - (100 / (1 + rs));
            rsiValues.Add(rsi);

            for (int i = period + 1; i < prices.Count; i++)
            {
                double change = prices[i] - prices[i - 1];
                double gain = change > 0 ? change : 0;
                double loss = change < 0 ? -change : 0;

                avgGain = (gain * alpha) + (avgGain * (1 - alpha));
                avgLoss = (loss * alpha) + (avgLoss * (1 - alpha));

                rs = avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
                rsiValues.Add(rsi);
            }

            Debug.WriteLine($"Calculating RSI {rsiValues.Last()} for {ApiId}");

            Rsi =  rsiValues.Last();
            return Rsi;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return 0;
        }
    }
}
