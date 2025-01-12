
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;
using Serilog;
using System.Collections.Generic;
using CryptoPortfolioTracker.Enums;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using LanguageExt;
using CoinGeckoFluentApi.Client;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Helpers;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;


public partial class DashboardService : ObservableObject, IDashboardService
{
    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(DashboardService).Name.PadRight(22));
    private readonly PortfolioService _portfolioService;
    private readonly IAssetService _assetService;
    private readonly INarrativeService _narrativeService;
    private readonly IPreferencesService _preferencesService;
    private readonly IAccountService _accountService;

    private DataPoint[] priceData;
    private DataPoint[] rsiData;


    public DashboardService(PortfolioService portfolioService, IAssetService assetService, INarrativeService narrativeService, IAccountService accountService, IPreferencesService preferencesService)
    {
        _portfolioService = portfolioService;
        _assetService = assetService;
        _preferencesService = preferencesService;
        _accountService = accountService;
        _narrativeService = narrativeService;
        
    }

    public PortfolioContext GetContext()
    {
        return _portfolioService.Context;
    }

    public async Task CalculateRsiAllCoins()
    {
        var coins = _portfolioService.Context.Coins.ToList();

        foreach(var coin in coins)
        {
            await coin.CalculateRsi();
            await Task.Delay(10);
        }
    }
   
    public double CalculateRSI(List<double> closingPrices, int period)
    {
        if (closingPrices == null || closingPrices.Count < period + 1)
            throw new ArgumentException("Insufficient data to calculate RSI");

        List<double> rsiValues = new List<double>();
        List<double> gains = new List<double>();
        List<double> losses = new List<double>();

        // Calculate initial average gains and losses
        for (int i = 1; i <= period; i++)
        {
            double change = closingPrices[i] - closingPrices[i - 1];
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

        // Calculate the alpha for EWMA
        double alpha = 1.0 / period;

        // Calculate RSI for the first period
        double rs = avgGain / avgLoss;
        double rsi = 100 - (100 / (1 + rs));
        rsiValues.Add(rsi);

        // Calculate RSI for the remaining periods using EWMA
        for (int i = period + 1; i < closingPrices.Count; i++)
        {
            double change = closingPrices[i] - closingPrices[i - 1];
            double gain = change > 0 ? change : 0;
            double loss = change < 0 ? -change : 0;

            avgGain = (gain * alpha) + (avgGain * (1 - alpha));
            avgLoss = (loss * alpha) + (avgLoss * (1 - alpha));

            rs = avgGain / avgLoss;
            rsi = 100 - (100 / (1 + rs));
            rsiValues.Add(rsi);
        }

        return rsiValues.Last();
    }

    public async Task<List<Coin>> GetTopWinners()
    {
        try
        {
            var context = _portfolioService.Context;
            
            var assets = await context.Assets
                .Include(x => x.Coin)
                .Where(x => x.Qty > 0 && x.Coin.Change24Hr > 0)
                .ToListAsync();

            List<Coin> topWinners = assets
                .GroupBy(x => x.Coin) // Group by Coin
                .Select(g => g.First().Coin) // Select the first asset's Coin from each group
                .OrderByDescending(x => x.Change24Hr) // Order by Change24Hr descending
                .Take(5) // Take the first 5
                .ToList();

            return topWinners ?? new();
        }
        catch (Exception ex)
        {
            return new();
        }
       
    }
    public async Task<List<Coin>> GetTopLosers()
    {
        try
        {
            var context = _portfolioService.Context;
            var assets = await context.Assets
                .Include(x => x.Coin)
                .Where(x => x.Qty > 0 && x.Coin.Change24Hr < 0)
                .ToListAsync();

            List<Coin> topLosers = assets
                .GroupBy(x => x.Coin) // Group by Coin
                .Select(g => g.First().Coin) // Select the first asset's Coin from each group
                .OrderBy(x => x.Change24Hr)
                .Take(5)
                .ToList();

            return topLosers ?? new();
        }
        catch (Exception ex)
        {

            return new();
        }
        
    }


    public Coin GetPriceLevelsFromContext(Coin coin)
    {
        return coin;
    }

    public double GetPortfolioValue()
    {
        return _assetService.GetTotalsAssetsValue();
    }

    public double GetCostBase()
    {
        return _assetService.GetTotalsAssetsCostBase();
    }


    public async Task<ObservableCollection<PiePoint>> GetPiePoints(string pieChartName)
    {
        // var values = new ObservableCollection<ObservablePoint>();
        int index = 0;

        var piePoints = new ObservableCollection<PiePoint>();

        try
        {
            var portfolioValue = _assetService.GetTotalsAssetsValue();

            if (pieChartName == "Portfolio")
            {
                // first get TOP 10 coins based on Rank
                var assets = (await _assetService.PopulateAssetTotalsList())
                    .Where(x => x.MarketValue > 0)
                    .OrderBy(x => x.Coin.Rank)
                    .Take(_preferencesService.GetMaxPieCoins())
                    .ToList();
                var sumOthersMarketValue = portfolioValue - assets.Sum(x => x.MarketValue);

                foreach (var asset in assets)
                {

                    if (asset is null || portfolioValue == 0) { continue; }

                    var perc = 100 * asset.MarketValue / portfolioValue;


                    if (!double.IsInfinity(perc))
                    {
                        var piePoint = new PiePoint
                        {
                            Value = perc,
                            Label = asset.Coin.Symbol
                        };
                        piePoints.Add(piePoint);
                        index += 1;
                    }

                }
                var perc2 = 100 * sumOthersMarketValue / portfolioValue;
                var piePoint2 = new PiePoint
                {
                    Value = perc2,
                    Label = "OTHERS"
                };
                piePoints.Add(piePoint2);


            }
            if (pieChartName == "Accounts")
            {
                var accounts = (await _accountService.PopulateAccountsList())
                    .Where(x => x.TotalValue > 0)
                    .OrderBy(x => x.TotalValue)
                    .ToList();

                foreach (var account in accounts)
                {
                    if (account is null || portfolioValue == 0) { continue; }

                    var perc = 100 * account.TotalValue / portfolioValue;

                    if (!double.IsInfinity(perc))
                    {
                        var piePoint = new PiePoint
                        {
                            Value = perc,
                            Label = account.Name
                        };
                        piePoints.Add(piePoint);
                        index += 1;
                    }

                }
            }
            if (pieChartName == "Narratives")
            {
                var narratives = (await _narrativeService.PopulateNarrativesList())
                    .Where(x => x.TotalValue > 0)
                    .OrderBy(x => x.TotalValue)
                    .ToList();

                foreach (var narrative in narratives)
                {
                    if (narrative is null || portfolioValue == 0) { continue; }

                    var perc = 100 * narrative.TotalValue / portfolioValue;

                    if (!double.IsInfinity(perc))
                    {
                        var piePoint = new PiePoint
                        {
                            Value = perc,
                            Label = narrative.Name
                        };
                        piePoints.Add(piePoint);
                        index += 1;
                    }

                }
            }

        }
        catch (Exception)
        {
            throw;
        }

        return piePoints;
    }



    public async Task<List<CapitalFlowPoint>> GetYearlyMutationsByTransactionKind(TransactionKind transactionKind)
    {
        var context = _portfolioService.Context;
        var dataPoints = new List<CapitalFlowPoint>();

        var mutations = await context.Mutations
            .Include(t => t.Transaction)
            .Where(x => x.Type == transactionKind)
            .GroupBy(g => g.Transaction.TimeStamp.Year)
            .Select(grouped => new
            {
                Year = grouped.Key.ToString(),
                Value = grouped.Sum(m => m.Qty * m.Price),
            })
            .OrderBy(t => t.Year)
            .ToListAsync();

        foreach (var mutation in mutations)
        {
            var dataPoint = new CapitalFlowPoint();
            dataPoint.Year = mutation.Year;
            dataPoint.Value = mutation.Value;
            dataPoints.Add(dataPoint);
        }

        return dataPoints;
    }

    public Portfolio GetPortfolio()
    {
        return _portfolioService.CurrentPortfolio;
    }
}





 
