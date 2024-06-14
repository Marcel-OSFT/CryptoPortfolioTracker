using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
using HttpClient = System.Net.Http.HttpClient;

namespace CryptoPortfolioTracker.Services;

public class GraphUpdateService : IGraphUpdateService
{
    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cts = new();
    private Task? timerTask;
    private readonly PortfolioContext coinContext;
    private readonly IGraphService _graphService;
    private int progressCounter;
    private int progressInterval;

    private static ILogger Logger { get; set; }
    public bool IsPaused { get; set; }

    public GraphUpdateService(IGraphService graphService, PortfolioContext portfolioContext)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphUpdateService).Name.PadRight(22));
        coinContext = portfolioContext;
        timer = new(System.TimeSpan.FromMinutes(1));
        IsPaused = false;
        _graphService = graphService;
    }

    public async Task Start()
    {
        await _graphService.LoadGraphFromJson();
        Logger.Information("GraphUpdateService started");
        timerTask = DoWorkAsync();
    }
    private async Task DoWorkAsync()
    {
        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (!IsPaused)
                {
                    Logger.Information("NextTick received");
                    await CheckForNewGraphData();
                }
                else
                {
                    Logger.Information("NextTick received => service paused");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Information("GraphUpdateService canceled");
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex, "GraphUpdateService stopped unexpected");
        }
    }
    public async Task Stop()
    {
        if (timerTask is null)
        {
            return;
        }
        cts.Cancel();
        cts.Dispose();

        Logger.Information("GraphUpdateService stopped");
    }
    public void Pause()
    {
        IsPaused = true;
        Logger.Information("GraphUpdateService Paused");
    }
    public void Continue()
    {
        IsPaused = false;
        Logger.Information("GraphUpdateService Continued");
    }
    private async Task<Result<bool>> CheckForNewGraphData()
    {
        int days;
        List<AssetTotals> assets;

        if (_graphService.HasHistoricalDataBuffer())
        {
            days = GetDaysFromBuffer();
            assets = await GetRemainingAssets();
        }
        else
        {
            if (_graphService.IsModificationRequested())
            {
                Logger.Information("Applying Modification => FromDate: {0}", _graphService.GetModifyFromDate());
                await _graphService.ApplyModification();
            }

            if (_graphService.HasDataPoints())
            {
                days = GetDaysBasedOnLatestDataPoint();
            }
            else
            {
                days = await GetDaysBasedOnOldestTransaction();
            }
            assets = await GetAssets();
        }

        if (days == 0 || !assets.Any())
        {
            Logger.Information("Historical Data up-to-date");
            return false;
        }
        Logger.Information("Historical Data Settings; days {0}, assets {1}", days, assets.Count);

        await AddInFlowData(days);
        await AddOutFlowData(days);
        await AddPortfolioValueData(assets, days);
        return true;
    }
    private async Task AddInFlowData(int days)
    {
        if (days == 0) { return; }
        var startDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days)).Date;
        var endDate = DateTime.UtcNow.Date;

        var deposits = await coinContext.Mutations
            .Include(t => t.Transaction)
            .Where(x => x.Transaction.TimeStamp.Date >= startDate
                && x.Transaction.TimeStamp.Date <= endDate
                && x.Type == Enums.TransactionKind.Deposit)
            .GroupBy(g => g.Transaction.TimeStamp.Date)
            .Select(grouped => new
            {
                Date = grouped.Key,
                InFlow = grouped.Sum(m => m.Qty * m.Price),
            })
            .OrderBy(t => t.Date)
            .ToListAsync();

        foreach (var deposit in deposits)
        {
            var dataPoint = new DataPoint();
            dataPoint.Date = DateOnly.FromDateTime(deposit.Date);
            dataPoint.Value = deposit.InFlow;
            _graphService.AddDataPointInFlow(dataPoint);
        }
        await _graphService.SaveGraphToJson();
    }
    private async Task AddOutFlowData(int days)
    {
        if (days == 0) { return; }
        var startDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days)).Date;
        var endDate = DateTime.UtcNow.Date;

        var withdraws = await coinContext.Mutations
            .Include(t => t.Transaction)
            .Where(x => x.Transaction.TimeStamp.Date >= startDate
                && x.Transaction.TimeStamp.Date <= endDate
                && x.Type == Enums.TransactionKind.Withdraw)
            .GroupBy(g => g.Transaction.TimeStamp.Date)
            .Select(grouped => new
            {
                Date = grouped.Key,
                OutFlow = grouped.Sum(m => m.Qty * m.Price),
            })
            .OrderBy(t => t.Date)
            .ToListAsync();

        foreach (var withdraw in withdraws)
        {
            var dataPoint = new DataPoint();
            dataPoint.Date = DateOnly.FromDateTime(withdraw.Date);
            dataPoint.Value = withdraw.OutFlow;

            _graphService.AddDataPointOutFlow(dataPoint);
        }
        await _graphService.SaveGraphToJson();
    }
    private async Task AddPortfolioValueData(List<AssetTotals> assets, int days)
    {
        var failed = false;
        progressCounter = 0;
        progressInterval = Convert.ToInt16(100.0/assets.Count);
        foreach (var asset in assets)
        {
            if (cts is null || cts.IsCancellationRequested || IsPaused)
            {
                return; //break in case GraphUpdateService has been canceled or paused
            }
            //in case of a pre-listing coin the actual Marketvalue is fixed as long as it is not listed yet.
            //This market value is already calculated and obtained in the above assetTotals query
            var getDataResult = await GetHistoricalPrices(asset);
            getDataResult.IfSucc(async s => {
                var historicalData = await CalculateAndPopulateHistoricalDataById(asset, days, s);
                _graphService.AddHistoricalDataToBuffer(historicalData);
            });
            getDataResult.IfFail(err => { failed = true; });

            await _graphService.SaveHistoricalDataBufferToJson();
            if (failed)
            {
                return;
            }
            progressCounter++;
            if (GraphicViewModel.Current is not null)
            {
                GraphicViewModel.Current.ProgressValue = Convert.ToInt16((100 * progressCounter / assets.Count));
            }
        }
        await CalculateAndStoreDataPoints(_graphService.GetHistoricalDataBuffer());
    }
    private async Task<HistoricalDataById> CalculateAndPopulateHistoricalDataById(AssetTotals asset, int days, MarketChartById chartData)
    {
        var data = new HistoricalDataById();

        data.Id = asset.Coin.ApiId;

        chartData.RemoveDuplicateLastDate();

        var dataSetChart = chartData.Prices.Skip(chartData.Prices.Length - days).ToArray();


        //for (var i = 0; i < chartData.Prices.Length; i++)
        var dateShift = 0;
        for (var i = 0; i < days; i++)
        {
            var expectedDate = DateOnly.FromDateTime(DateTime.UtcNow.Subtract(TimeSpan.FromDays(days - 1 - i)));
            var date = DateOnly.FromDateTime(DateTime.UnixEpoch.AddMilliseconds(Convert.ToDouble(dataSetChart[i - dateShift][0])));

            if (date.Equals(expectedDate))
            {
                var price = (double)dataSetChart[i - dateShift][1];
                data.Dates.Add(date);
                data.Prices.Add(price);
                var historicalQty = await GetHistoricalQtyByDate(date, asset);
                data.Quantities.Add(historicalQty);
            }
            else if (expectedDate.CompareTo(date) < 0)// No Data for this date
            {
                dateShift++;
                data.Dates.Add(expectedDate);
                data.Prices.Add(0);
                var historicalQty = await GetHistoricalQtyByDate(date, asset);
                data.Quantities.Add(historicalQty);
            }
            else // incorrect data => first date is older then expected date
            {
                break;
            }
            //calculate the asset qty on each day taking the Transactions/mutations done into account
            //then add this qty to the data-array
        }
        return data;
    }
    private async Task<bool> CalculateAndStoreDataPoints(List<HistoricalDataById> dataByIds)
    {
        var nrOfPointsAdded = 0;
        try
        {
            for (var i = 0; i < dataByIds.First().Dates.Count; i++)
            {
                var dataPoint = new DataPoint();
                var totalValueByDate = dataByIds.Sum(x => x.Quantities[i] * x.Prices[i]);
                dataPoint.Date = dataByIds.First().Dates[i];
                dataPoint.Value = Math.Round(totalValueByDate, 0);
                _graphService.AddDataPointPortfolio(dataPoint);
                nrOfPointsAdded++;
            }
        }
        catch (Exception ex)
        {
            Logger.Warning("Historical Data {0} of {1} data points added", nrOfPointsAdded, dataByIds.First().Dates.Count);
        }
        finally
        {
            await _graphService.SaveGraphToJson();
            _graphService.ClearHistoricalDataBuffer();
            Logger.Information("Historical Data updated and saved");
            //MainPage.Current.IsChartLoaded = portfolioGraph.DataPointsPortfolio.Any();
            if (GraphicViewModel.Current is not null ) GraphicViewModel.Current.IsUpdating = false;
        }
        return true;
    }
    private async Task<Result<MarketChartById>> GetHistoricalPrices(AssetTotals asset)
    {
        var marketChart = new MarketChartById();
        var daysToGet = 0;

        await marketChart.LoadMarketChartJson(asset.Coin.ApiId);
        if (marketChart is null || marketChart.Prices is null || marketChart.Prices.Length == 0)
        {
            daysToGet = 365;
            Logger.Information("No MarketChartJson available for {0}", asset.Coin.ApiId);
        }
        else
        {
            var lastChartDate = marketChart.EndDate().ToDateTime(TimeOnly.Parse("01:00 AM"));
            daysToGet = DateTime.UtcNow.Subtract(lastChartDate).Days;
            if (daysToGet > 0)
            {
                Logger.Information("MarketChartJson needs additional data for the last {0} days => {1}", daysToGet, asset.Coin.ApiId);
            }
            else
            {
                Logger.Information("MarketChartJson up-to-date => {0}", asset.Coin.ApiId);
            }
        }

        if (daysToGet > 0 &&  (asset.Coin.Name.Length <= 12 || (asset.Coin.Name.Length > 12
            && asset.Coin.Name.Substring(asset.Coin.Name.Length - 12) != "_pre-listing")))
        {
            //No data yet, so get/save History for 365 days and set marketChart.Prices for requested amount of days
            await DelayAndShowProgress(12);

            var result = await GetHistoricalPricesFromGecko(asset, daysToGet);
            result.IfSucc(s => marketChart.AddPrices(s.Prices));
            result.IfFail(err => marketChart = null);
        }
        else if (daysToGet > 0)
        {
            CreateMarketChartForPrelistingCoin(asset, daysToGet)
                .IfSucc(s => marketChart.AddPrices(s.Prices));   // s
        }

        if (marketChart is not null && marketChart.Prices is not null && marketChart.Prices.Length > 0)
        {
            await marketChart.SaveMarketChartJson(asset.Coin.ApiId);
        }
        return marketChart ?? new MarketChartById();
    }

    private async Task DelayAndShowProgress(int delayInSeconds)
    {
        var startValue = progressCounter * progressInterval;
        var stepSize = (double)progressInterval/delayInSeconds;
        for (int i = 0; i < delayInSeconds; i++)
        {
            await Task.Delay(1000);
            if (GraphicViewModel.Current is not null)
            {
                //check/set 'isFinishedLoading' to false to show message
                GraphicViewModel.Current.IsUpdating = true;
                var newValue = startValue + (stepSize * i);
                GraphicViewModel.Current.ProgressValue = Convert.ToInt16((double)newValue);
            }
        }
        
    }

    private Result<MarketChartById> CreateMarketChartForPrelistingCoin(AssetTotals asset, int days)
    {
        var marketChart = new MarketChartById();
        marketChart.Prices = new decimal?[days][];

        var data = new HistoricalDataById();

        data.Id = asset.Coin.ApiId;

        decimal price = (decimal)asset.Coin.Price;

        for (var i = 0; i < days; i++)
        {
            var date = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days - 1 - i)).Date;
            marketChart.Prices[i] = new decimal?[2] { (decimal)date.Subtract(DateTime.UnixEpoch).TotalMilliseconds, price };
        }

        Logger.Information("Created MarketChart for pre-listing coin; {0}", asset.Coin.ApiId);

        return marketChart ?? new MarketChartById();
    }
    private async Task<Result<MarketChartById>> GetHistoricalPricesFromGecko(AssetTotals asset, int days)
    {
        var Retries = 0;
        var TotalRequests = 0;

        var tokenSource2 = new CancellationTokenSource();
        var cancellationToken = tokenSource2.Token;

        var strategy = new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<System.Exception>(),
            MaxRetryAttempts = 3,
            Delay = System.TimeSpan.FromSeconds(60), // Wait between each try
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                Logger.Debug(exception, "Getting Historical Data; OnRetry ({0})", Retries.ToString());
                Retries++;
                return default;
            }
        }).Build();


        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();

        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        System.Exception? error = null;
        MarketChartById? marketChart = null;

        while (!cancellationToken.IsCancellationRequested && !IsPaused)
        {
            TotalRequests++;
            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    Logger.Debug("Getting Historical Data; (Retries: {0})", Retries.ToString());
                    marketChart = await coinsClient.Coins[asset.Coin.ApiId].MarketChart
                        .Interval("daily")
                        .Precision("full")
                        .Days(days.ToString())
                        .VsCurrency("usd")
                        .GetAsync<MarketChartById>(token);
                }, cancellationToken);

                Logger.Information("Received Historical Data; {0}", asset.Coin.ApiId);
            }
            catch (System.Exception ex)
            {
                Logger.Warning(ex, "Getting Historical Data failed after {0} requests - {1}", TotalRequests);
                error = ex;
            }
            finally
            {
                tokenSource2.Cancel();
                tokenSource2.Dispose();
            }
        }
        if (error != null)
        {
            return new Result<MarketChartById>(error);
        }

        return marketChart ?? new Result<MarketChartById>(new NullReferenceException());
    }
    private async Task<double> GetHistoricalQtyByDate(DateOnly _date, AssetTotals asset)
    {
        var historicalQty = asset.Qty;
        var date = _date.ToDateTime(TimeOnly.Parse("01:00 AM"));

        var mutations = await coinContext.Mutations
            .Where(m => m.Asset.Coin.ApiId.ToLower() == asset.Coin.ApiId.ToLower())
            .Include(t => t.Transaction)
            .Where(t => t.Transaction.TimeStamp.Date >= date.Date && t.Transaction.TimeStamp.Date <= DateTime.UtcNow)
            .ToListAsync();

        if (mutations is not null && mutations.Count > 0)
        {
            var QtyIn = mutations.Where(x => x.Direction == Enums.MutationDirection.In).Sum(x => x.Qty);
            var QtyOut = mutations.Where(x => x.Direction == Enums.MutationDirection.Out).Sum(x => x.Qty);

            var delta = QtyIn - QtyOut;
            historicalQty = asset.Qty - delta;

        }

        return historicalQty;
    }
    private async Task<List<AssetTotals>> GetAssets()
    {
        var assets = await coinContext.Assets
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => new AssetTotals
                {
                    Qty = assetGroup.Sum(x => x.Qty),
                    Coin = assetGroup.Key,
                })
                .ToListAsync();

        return assets;
    }
    private async Task<int> GetDaysBasedOnOldestTransaction()
    {
        var tx = await coinContext.Transactions.OrderBy(x => x.TimeStamp).FirstAsync();
        var days = DateTime.UtcNow.Subtract(tx.TimeStamp).Days + 1;

        return days;
    }
    private int GetDaysBasedOnLatestDataPoint()
    {
        var lastDataPointDate = _graphService.GetLatestDataPointDate();
        var days = DateTime.UtcNow.Subtract(lastDataPointDate.ToDateTime(TimeOnly.Parse("01:00 AM"))).Days;

        return days;
    }
    
    private async Task<List<AssetTotals>> GetRemainingAssets()
    {
        var assets = await coinContext.Assets
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => new AssetTotals
                {
                    Qty = assetGroup.Sum(x => x.Qty),
                    Coin = assetGroup.Key,
                })
                .ToListAsync();
        //Remove from assets the ones that are already in the buffer
        var buffer = _graphService.GetHistoricalDataBuffer();
        foreach (var item in buffer)
        {
            var asset = assets.Find(x => x.Coin.ApiId == item.Id);
            assets.Remove(asset);
        }

        return assets;
    }
    private int GetDaysFromBuffer()
    {
        // In case continuation happens after a (multiple) day-change,
        // aditional day(s) needs to be added to line-up with the already obtained data in the buffer
        
        var days = _graphService.GetHistoricalDataBufferDatesCount();
        var latestDate = _graphService.GetHistoricalDataBufferLatestDate();

        var endDateDifference = DateTime.UtcNow.Subtract(latestDate.ToDateTime(TimeOnly.Parse("01:00 AM"))).Days;
        if (endDateDifference > 0)
        {
            days += endDateDifference;
        }
        
        return days;
    }
    
}

