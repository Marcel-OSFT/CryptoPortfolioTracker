
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using Serilog;
using System.Collections.Generic;
using CryptoPortfolioTracker.Enums;
using LiveChartsCore.Defaults;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using CryptoPortfolioTracker.Controls;
using LiveChartsCore.SkiaSharpView.WinUI;
using LanguageExt;


namespace CryptoPortfolioTracker.Services;


public partial class DashboardService : ObservableObject, IDashboardService
{
    private static ILogger Logger { get; set; }
    public readonly PortfolioContext coinContext;
    private readonly IAssetService _assetService;
    private readonly INarrativeService _narrativeService;
    private readonly IPreferencesService _preferencesService;
    private readonly IAccountService _accountService;




    public DashboardService(PortfolioContext portfolioContext, IAssetService assetService, INarrativeService narrativeService, IAccountService accountService, IPreferencesService preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphService).Name.PadRight(22));
        coinContext = portfolioContext;
        _assetService = assetService;
        _preferencesService = preferencesService;
        _accountService = accountService;
        _narrativeService = narrativeService;
    }

    public ObservableCollection<Coin> GetTopWinners()
    {
        var topWinners = coinContext.Assets
            .Include(x => x.Coin)
            .Where(x => x.Qty > 0 && x.Coin.Change24Hr > 0)
            .GroupBy(x => x.Coin) // Group by Coin
            .Select(g => g.First().Coin) // Select the first asset's Coin from each group
            .ToList();

        if (topWinners is null)
        {
            return new();
        }
        else
        {
            return new(topWinners.OrderByDescending(x => x.Change24Hr).Take(5));
        }

    }
    public ObservableCollection<Coin> GetTopLosers()
    {
        var topLosers = coinContext.Assets
            .Include(x => x.Coin)
            .Where(x => x.Qty > 0 && x.Coin.Change24Hr < 0)
            .GroupBy(x => x.Coin) // Group by Coin
            .Select(g => g.First().Coin) // Select the first asset's Coin from each group
            .ToList();

        if (topLosers is null)
        {
            return new();
        }
        else
        {
            return new(topLosers.OrderBy(x => x.Change24Hr).Take(5));
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
        //var startDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days)).Date;
        //var endDate = DateTime.UtcNow.Date;
        var dataPoints = new List<CapitalFlowPoint>();

        var mutations = await coinContext.Mutations
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

}





 
