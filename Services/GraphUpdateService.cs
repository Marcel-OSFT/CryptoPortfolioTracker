using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using Windows.Devices.WiFi;
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
    private double progressCounter;
    private double progressInterval;

    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphUpdateService).Name.PadRight(22));
    public bool IsPaused { get; set; }

    public GraphUpdateService(IGraphService graphService, PortfolioContext portfolioContext)
    {
        coinContext = portfolioContext;
        timer = new(System.TimeSpan.FromMinutes(1));
        IsPaused = false;
        _graphService = graphService;
    }


    private MarketChartById CheckAndFixMarketChart(MarketChartById additionalChart, MarketChartById fullChart, DateTime startDate)
    {
        var additionalPriceList = additionalChart.GetPriceList();
        var fullPriceList = fullChart.GetPriceList();
        var checkedChart = new MarketChartById();

        var fixCounter = 0;

        for (DateTime date = startDate; date <= DateTime.UtcNow; date = date.AddDays(1))
        {
            var dataPointToCheck = additionalPriceList.Where(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date == date.Date).FirstOrDefault();

            if (dataPointToCheck is not null)
            {
                continue; //and check next
            }
            else
            {
                var value = 0.0;
                var adjacent = additionalPriceList.Where(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date == date.AddDays(-1).Date).FirstOrDefault();

                if (adjacent is null && fullPriceList is not null && fullPriceList.Count > 0)
                {
                    adjacent = fullPriceList.Where(x => x.Date.ToDateTime(TimeOnly.Parse("01:00 AM")).Date == date.AddDays(-1).Date).FirstOrDefault();

                }

                if (adjacent is not null )
                {
                    value = adjacent.Value; 
                }
                fixCounter++;
                Logger.Information("Missing DataPoint for {0} => added with value {1}", date.Date.ToString("dd-MM-yyyy"), value);
                var dataPoint = new DataPoint { Date = DateOnly.FromDateTime(date), Value = value };
                additionalPriceList.Add(dataPoint);
            }
        }
        Logger.Information("MarketChart CheckAndFix - Done. {0} entries needed fix.", fixCounter);


        checkedChart.FillPricesArray(additionalPriceList);

        return checkedChart;

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

                //*** CoinGecko provides data for 'only' the last 365 days. If oldest Transaction is beyond 365 days 
                //*** an initial starting Point needs to be set as first portfolio value
                if (days>365)
                {
                    ObtainFirstPortfolioValue();
                    days = 365;
                }


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

        //*** In case adding new values to PortfolioValues has failed during a previous loop, then
        //*** it might be that InflowData already has been added in the previous loop and
        //*** will be out-of-sync 
        //*** so, check also latest date and add starting from that date instead of the expected 'startdate' 

        var lastEntry = _graphService.GetLatestDataPointInFlow();
        var lastEntryDate = DateOnly.MinValue;

        if (lastEntry is not null)
        {
            lastEntryDate = lastEntry.Date;
        }

        startDate = lastEntryDate > DateOnly.FromDateTime(startDate.Date) ? lastEntryDate.ToDateTime(new TimeOnly(1,0)).Date : startDate;

        // var endDate = DateTime.UtcNow.Date;

        var deposits = await coinContext.Mutations
            .Include(t => t.Transaction)
            .Where(x => x.Transaction.TimeStamp.Date >= startDate
                //&& x.Transaction.TimeStamp.Date <= endDate
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
        //await _graphService.SaveGraphToJson();
    }
    private async Task AddOutFlowData(int days)
    {
        if (days == 0) { return; }
        var startDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days)).Date;
        // var endDate = DateTime.UtcNow.Date;

        //*** In case adding new values to PortfolioValues has failed during a previous loop, then
        //*** it might be that InflowData already has been added in the previous loop and
        //*** will be out-of-sync 
        //*** so, check also latest date and add starting from that date instead of the expected 'startdate' 

        var lastEntry = _graphService.GetLatestDataPointOutFlow();
        var lastEntryDate = DateOnly.MinValue;

        if (lastEntry is not null)
        {
            lastEntryDate = lastEntry.Date;
        }
        startDate = lastEntryDate > DateOnly.FromDateTime(startDate.Date) ? lastEntryDate.ToDateTime(new TimeOnly(1, 0)).Date : startDate;

        var withdraws = await coinContext.Mutations
            .Include(t => t.Transaction)
            .Where(x => x.Transaction.TimeStamp.Date >= startDate
               // && x.Transaction.TimeStamp.Date <= endDate
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
        //await _graphService.SaveGraphToJson();
    }
    private async Task AddPortfolioValueData(List<AssetTotals> assets, int days)
    {
        var failed = false;
        progressCounter = 0;
        progressInterval = (100.0/assets.Count);
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
            if (DashboardViewModel.Current is not null)
            {
                DashboardViewModel.Current.ProgressValueGraph = Convert.ToInt16((100 * progressCounter / assets.Count));
            }
        }
        await CalculateAndStoreDataPoints(_graphService.GetHistoricalDataBuffer());

    }
    private async Task<HistoricalDataByIdRev> CalculateAndPopulateHistoricalDataById(AssetTotals asset, int days, MarketChartById chartData)
    {
        var indexPos = 0;
        
        var data = new HistoricalDataByIdRev();
        if (chartData.Prices is null) { return data; }

        data.Id = asset.Coin.ApiId;

        chartData.RemoveDuplicateLastDate();
        var firstDate = DateOnly.FromDateTime(DateTime.UtcNow.Subtract(TimeSpan.FromDays(days - 1)));

        var dataSetChart = chartData.Prices.Where(x => DateOnly.FromDateTime(DateTime.UnixEpoch.AddMilliseconds(Convert.ToDouble(x[0]))).CompareTo(firstDate) >= 0).ToArray();

        var dateShift = 0;

        try
        {
            for (var i = 0; i < days; i++)
            {
                indexPos = i - dateShift;

                var expectedDate = DateOnly.FromDateTime(DateTime.UtcNow.Subtract(TimeSpan.FromDays(days - 1 - i)));
                var date = DateOnly.FromDateTime(DateTime.UnixEpoch.AddMilliseconds(Convert.ToDouble(dataSetChart[i - dateShift][0])));

                if (date.Equals(expectedDate))
                {
                    var point = new DataPoint();
                    var price = (double)(dataSetChart[i - dateShift][1] ?? 0);
                    var historicalQty = await GetHistoricalQtyByDate(date, asset);
                    point.Date = date;
                    point.Value = price * historicalQty;

                    //*** if GraphUpdate has been postponed into one of the next days, then it could be that a certain 
                    //*** datapoint has already been entered into the list
                    //*** so, check to prevent duplicated
                    var entryIn = data.DataPoints.Where(x => x.Date == point.Date).FirstOrDefault();

                    if (entryIn is null)
                    {
                        data.DataPoints.Add(point);
                    }
                }
                else if (expectedDate.CompareTo(date) < 0)// No Data for this date
                {
                    var point = new DataPoint();
                    point.Date = expectedDate;
                    point.Value = 0;
                    dateShift++;
                    //*** if GraphUpdate has been postponed into one of the next days, then it could be that a certain 
                    //*** datapoint has already been entered into the list
                    //*** so, check to prevent duplicated
                    var entryIn = data.DataPoints.Where(x => x.Date == point.Date).FirstOrDefault();

                    if (entryIn is null)
                    {
                        data.DataPoints.Add(point);
                    }
                }
                else // incorrect data => missing datapoint which now should be fixed by the CheckAndFixMarketChart method
                {
                    break;
                }
                //calculate the asset qty on each day taking the Transactions/mutations done into account
                //then add this qty to the data-array
            }
            return data;  
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"CalculateAndPopulateHistoricalDataById failed for " + asset.Coin.Name + " at index " + indexPos.ToString() + " date-shift = " + dateShift.ToString()    );
            return new HistoricalDataByIdRev();
        }
        
    }
    private async Task<bool> CalculateAndStoreDataPoints(List<HistoricalDataByIdRev> dataByIds)
    {
        var nrOfPointsAdded = 0;
        try
        {
            //for (var i = 0; i < dataByIds.First().Dates.Count; i++)
            //{
            //    var dataPoint = new DataPoint();
            //    var totalValueByDate = dataByIds.Sum(x => x.Quantities[i] * x.Prices[i]);
            //    dataPoint.Date = dataByIds.First().Dates[i];
            //    dataPoint.Value = Math.Round(totalValueByDate, 0);
            //    _graphService.AddDataPointPortfolio(dataPoint);
            //    nrOfPointsAdded++;
            //}

            var dates = dataByIds.First().DataPoints.Select(x => x.Date).ToList();

            foreach (var date in dates)
            {
                var totalValueByDate = dataByIds
                    .SelectMany(x => x.DataPoints)
                    .Where(dp => dp.Date == date)
                    .Sum(dp => dp.Value);

                var dataPoint = new DataPoint
                {
                    Date = date,
                    Value = totalValueByDate
                };

                _graphService.AddDataPointPortfolio(dataPoint);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning("Historical Data {0} of {1} data points added", nrOfPointsAdded, dataByIds.First().DataPoints.Count);
        }
        finally
        {
            await _graphService.SaveGraphToJson();
            _graphService.ClearHistoricalDataBuffer();
            Logger.Information("Historical Data updated and saved");
            if (DashboardViewModel.Current is not null) DashboardViewModel.Current.IsUpdatingGraph = false;
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

        if (daysToGet > 0 && (asset.Coin.Name.Length <= 12 || (asset.Coin.Name.Length > 12
            && asset.Coin.Name.Substring(asset.Coin.Name.Length - 12) != "_pre-listing")))
        {
            //No data yet, so get/save History for 365 days and set marketChart.Prices for requested amount of days
            Task delay = Task.Run(() => DelayAndShowProgress(true));

            var result = await GetHistoricalPricesFromGecko(asset, daysToGet, marketChart ?? new MarketChartById());
            result.IfSucc(s => marketChart.AddPrices(s.Prices));
            result.IfFail(err => marketChart = null);

            delay.Wait();
        }
        else if (daysToGet > 0)
        {
            CreateMarketChartForPrelistingCoin(asset, daysToGet)
                .IfSucc(s => marketChart.AddPrices(s.Prices));   // s
        }

        if (daysToGet == 0) await DelayAndShowProgress(false);

        if (marketChart is not null && marketChart.Prices is not null && marketChart.Prices.Length > 0)
        {
            await marketChart.SaveMarketChartJson(asset.Coin.ApiId);
        }
        return marketChart ?? new MarketChartById();
    }


    private async Task DelayAndShowProgress(bool isStepping)
    {
        var startValue = progressCounter * progressInterval;
        var nrOfSteps = 10;
        var stepSize = (double)progressInterval / nrOfSteps;
        
        if (isStepping)
        {
            for (int i = 0; i < nrOfSteps; i++)
            {
                await Task.Delay(1000);

                if (DashboardViewModel.Current is not null)
                {
                    // Use the DispatcherQueue to update the UI thread
                    MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        // Check/set 'isFinishedLoading' to false to show message
                        DashboardViewModel.Current.IsUpdatingGraph = true;
                        var newValue = (int)Math.Floor(startValue + (stepSize * i));
                        DashboardViewModel.Current.ProgressValueGraph = newValue;
                    });
                }
            }
        }
        else
        {
            if (DashboardViewModel.Current is not null)
            {
                // Check/set 'isFinishedLoading' to false to show message
                DashboardViewModel.Current.IsUpdatingGraph = true;
                DashboardViewModel.Current.ProgressValueGraph = (int)Math.Floor(startValue);
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
    private async Task<Result<MarketChartById>> GetHistoricalPricesFromGecko(AssetTotals asset, int days, MarketChartById fullChart)
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
        MarketChartById? additionalMarketChart = null;

        while (!cancellationToken.IsCancellationRequested && !IsPaused)
        {
            TotalRequests++;
            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    Logger.Debug("Getting Historical Data; (Retries: {0})", Retries.ToString());
                    
                    additionalMarketChart = await coinsClient.Coins[asset.Coin.ApiId].MarketChart
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

        Logger.Information("Checking MarketChart for {0}", asset.Coin.ApiId);
        var checkedMarketChart = CheckAndFixMarketChart(additionalMarketChart ?? new MarketChartById(), fullChart, DateTime.UtcNow.Subtract(TimeSpan.FromDays(days - 1)));


        return checkedMarketChart ?? new Result<MarketChartById>(new NullReferenceException());
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
        var tx = await coinContext.Transactions.OrderBy(x => x.TimeStamp).FirstOrDefaultAsync();
        var days =  tx is not null ? DateTime.UtcNow.Subtract(tx.TimeStamp).Days + 1 : 0;

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
    
    private void ObtainFirstPortfolioValue()
    {
        var beforeDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(365)).Date;

        var sumDeposits = coinContext.Mutations
            .Include(t => t.Transaction)
            .Where(x => x.Type == Enums.TransactionKind.Deposit && x.Transaction.TimeStamp.Date < beforeDate)
            .Sum(x => x.Qty * x.Price);

        var sumWithDraws = coinContext.Mutations
                    .Include(t => t.Transaction)
                    .Where(x => x.Type == Enums.TransactionKind.Withdraw && x.Transaction.TimeStamp.Date < beforeDate)
                    .Sum(x => x.Qty * x.Price);

        _graphService.AddDataPointInFlow(new DataPoint { Date = DateOnly.FromDateTime(beforeDate), Value = sumDeposits });
        _graphService.AddDataPointOutFlow(new DataPoint { Date = DateOnly.FromDateTime(beforeDate), Value = sumWithDraws });
        _graphService.AddDataPointPortfolio(new DataPoint { Date = DateOnly.FromDateTime(beforeDate), Value = sumDeposits - sumWithDraws });

    }




}

