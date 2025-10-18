
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
using System.Diagnostics;

namespace CryptoPortfolioTracker.Services;


public partial class DashboardService : ObservableObject, IDashboardService
{
    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(DashboardService).Name.PadRight(22));
    private readonly PortfolioService _portfolioService;
    private readonly IAssetService _assetService;
    private readonly INarrativeService _narrativeService;
    private readonly IPreferencesService _preferencesService;
    private readonly IAccountService _accountService;

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

    public async Task CalculateIndicatorsAllCoins()
    {
        var coins = _portfolioService.Context.Coins.ToList();

        foreach (var coin in coins)
        {
            await coin.CalculateRsi();
            await coin.CalculateMa();
            await Task.Delay(10);
        }
    }
    public async Task CalculateRsiAllCoins()
    {
        var coins = _portfolioService.Context.Coins.ToList();

        foreach (var coin in coins)
        {
            await coin.CalculateRsi();
            await Task.Delay(10);
        }
    }
    public async Task CalculateMaAllCoins()
    {
        var coins = _portfolioService.Context.Coins.ToList();

        foreach (var coin in coins)
        {
            await coin.CalculateMa();
            await Task.Delay(10);
        }
    }


    public async Task<List<Coin>> GetTopWinners()
    {
        try
        {
            var context = _portfolioService.Context;
            
            var coins = await context.Assets
                .AsNoTracking()
                .Where(x => x.Qty > 0 && x.Coin.Change24Hr > 0)
                .Include(x => x.Coin)
                .GroupBy(x => x.Coin) // Group by Coin
                .ToListAsync();

            List<Coin> topWinners = coins
                .Select(g => g.First().Coin) // Select the first Coin from each group's asset
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

            var coins = await context.Assets
                .AsNoTracking()
                .Where(x => x.Qty > 0 && x.Coin.Change24Hr < 0)
                .Include(x => x.Coin)
                .GroupBy(x => x.Coin) // Group by Coin
                .ToListAsync();

            List<Coin> topLosers = coins
                .Select(g => g.First().Coin) // Select the first Coin from each group's asset
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

    public async Task EvaluatePriceLevels()
    {
        var coins = _portfolioService.Context.Coins
            .AsNoTracking()
            .ToList();

        foreach (var coin in coins)
        {
            coin.EvaluatePriceLevels(coin.Price);
        }
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
            //minimum value visible in Pie is 0.01%. if it is 0.00 % it will be shown on every pie
            // which we don't want.

            var threshold = Math.Ceiling(0.0001 * portfolioValue);

            if (pieChartName == "PortfolioPie")
            {
                // first get TOP 10 coins based on Rank
                var assets = (await _assetService.PopulateAssetTotalsList())
                    .Where(x => x.MarketValue > threshold)
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
                var percOthers = 100 * sumOthersMarketValue / portfolioValue;
                if (percOthers > threshold)
                {
                    var piePoint2 = new PiePoint
                    {
                        Value = percOthers,
                        Label = "OTHERS"
                    };
                    piePoints.Add(piePoint2);
                }
            }
            if (pieChartName == "AccountsPie")
            {
                var accounts = (await _accountService.PopulateAccountsList())
                    .Where(x => x.TotalValue > threshold)
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
            if (pieChartName == "NarrativesPie")
            {
                var narratives = (await _narrativeService.PopulateNarrativesList())
                    .Where(x => x.TotalValue > threshold)
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
            .AsNoTracking()
            .Where(x => x.Type == transactionKind)
            .Include(t => t.Transaction)
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





 
