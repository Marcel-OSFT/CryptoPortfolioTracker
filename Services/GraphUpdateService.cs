using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace CryptoPortfolioTracker.Services;

public interface IGraphUpdateService
{
    public bool IsUpdating { get; }
    public void Start();
    public void Stop();
    public void Pause(bool isDisconnecting = false);
    public void Resume();
}

public class GraphUpdateService : IGraphUpdateService
{
    private readonly IGraphService _graphService;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IAppState _appState;
    private readonly IMessenger _messenger;
    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cts = new();
    private Task? timerTask;
    private Task? resumeTask;

    private UpdateContext currentContext;
    private PortfolioContext MainContext => _appState.Context;
    private UpdateContext UpdateContext => _appState.UpdateContext;
    private string PortfolioSignature => _appState.CurrentPortfolio.Signature;

    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphUpdateService).Name.PadRight(22));
    public bool IsPauseRequested { get; private set; }
    public bool IsUpdating { get; private set; }

    public GraphUpdateService(IGraphService graphService, IAppState appState, IExchangeRateService exchangeRateService , IMessenger messenger)
    {
        timer = new(System.TimeSpan.FromMinutes(5));
        IsPauseRequested = false;
        _graphService = graphService;
        _exchangeRateService = exchangeRateService;
        _messenger = messenger;
        _appState = appState;
        _appState.ContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(MainContext));
        };
        _appState.UpdateContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(UpdateContext));
        }; 
        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PortfolioSignature));
        };
    }

    public void Start()
    {
        Logger.Information("GraphUpdateService started");
        currentContext = UpdateContext;
        timerTask = DoWorkAsync();
    }
    private async Task DoWorkAsync()
    {
        try
        {
            await _exchangeRateService.LoadDailyExchangeRates(await GetOldestTransaction());
            await _graphService.LoadGraphFromJson(PortfolioSignature);
            await Task.Delay(10000);
            if (didScheduledTaskComplete())
            {
                await CheckForNewGraphData();
            }

            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (!IsPauseRequested && didScheduledTaskComplete())
                {
                    if (resumeTask == null || resumeTask.IsCompleted )
                    {
                        resumeTask = null;
                        Logger.Information("NextTick received");
                        await CheckForNewGraphData();
                    }
                    else
                    {
                        Logger.Information("NextTick received => service waiting for resumeTask to complete");
                    };
                }
                else if (IsPauseRequested)
                {
                    Logger.Information("NextTick received => service paused");
                }
                else
                {
                    Logger.Information("NextTick received => Scheduled Update Task not yet completed for today");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Information("GraphUpdateService cancelled");
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex, "GraphUpdateService stopped unexpected");
        }
    }
    public void Stop()
    {
        if (timerTask is null)
        {
            return;
        }
        cts.Cancel();
        cts.Dispose();
        IsUpdating = false;

        Logger.Information("GraphUpdateService stopped");
    }
    public void Pause(bool isDisconnecting = false)
    {
        IsPauseRequested = true;
        Logger.Information("GraphUpdateService Paused");
    }

    public void Resume()
    {
        IsPauseRequested = false;
        if (currentContext != UpdateContext)
        {   
            resumeTask = Task.Run(async () =>
            {
                Logger.Information("GraphUpdateService continued with new context");
                currentContext = UpdateContext;
                await _graphService.LoadGraphFromJson(PortfolioSignature);
            });
        }
        else
        {
            Logger.Information("PriceUpdateService continued with existing context");
        }
    }
    private bool didScheduledTaskComplete()
    {
        using (TaskService ts = new TaskService())
        {
            TaskFolder folder = ts.GetFolder("\\");
            var task = folder.Tasks.Where(t => t.Name == AppConstants.ScheduledTaskName).FirstOrDefault();
            
            if (task != null)
            {
                if (task.LastRunTime.Date == DateTime.Today && task.LastTaskResult == 0)
                {
                    Logger.Information("{0} has completed its task today", AppConstants.ScheduledTaskName);
                    return true;
                }
                else if (task.LastTaskResult != 0)
                {
                    Logger.Information("{0} has failed to run to completion", AppConstants.ScheduledTaskName);
                }
                else if (task.State == TaskState.Running)
                {
                    Logger.Information("{0} is running", AppConstants.ScheduledTaskName);
                }
                else
                {
                    Logger.Information("{0} not yet executed", AppConstants.ScheduledTaskName);
                }
            }
            return false;
        }
    }
    private async Task<Result<bool>> CheckForNewGraphData()
    {
        int days;
        List<AssetTotals> assets;
        IsUpdating = true;
        try
        {
            if (_graphService.IsModificationRequested())
            {
                Logger.Information("Applying Modification => FromDate: {0}", _graphService.GetModifyFromDate());
                await _graphService.ApplyModification(PortfolioSignature);
            }

            if (_graphService.HasDataPoints())
            {
                days = GetDaysBasedOnLatestDataPoint();
            }
            else
            {
                days = await GetDaysBasedOnOldestTransaction();

                // Existing 365-day fallback is used only when old market-chart data is unavailable.
                int oldestMarketChartDays = await GetDaysBasedOnOldestMarketChartData();

                if (oldestMarketChartDays > 0)
                {
                    days = Math.Max(days, oldestMarketChartDays);
                }

                if (days > 365 && oldestMarketChartDays <= 365)
                {
                    ObtainFirstPortfolioValue();
                    days = 365;
                }
            }
            assets = await GetAssets();

            if (days == 0 || !assets.Any())
            {
                Logger.Information("Historical Data up-to-date");
                return true;
            }
            Logger.Information("Historical Data Settings; days {0}, assets {1}", days, assets.Count);

            


            await AddInFlowData(days);
            await AddOutFlowData(days);
            await AddPortfolioValueData(assets, days);

            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            {
                _messenger.Send(new GraphUpdatedMessage());
            });

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "CheckForNewGraphData failed.");
            return new Result<bool>(ex);
        }
        finally
        {
            IsUpdating = false;
        }
    }

   

    private async Task AddInFlowData(int days)
    {
        await App.UpdateSemaphore.WaitAsync();
        try
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

            startDate = lastEntryDate > DateOnly.FromDateTime(startDate.Date) ? lastEntryDate.ToDateTime(new TimeOnly(1, 0)).Date : startDate;

            // var endDate = DateTime.UtcNow.Date;
            var context = UpdateContext;
            var deposits = await context.Mutations
                .AsNoTracking()
                .Where(x => x.Transaction.TimeStamp.Date >= startDate
                    //&& x.Transaction.TimeStamp.Date <= endDate
                    && x.Type == Enums.TransactionKind.Deposit)
                .Include(t => t.Transaction)
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
        }
        catch (Exception ex)
        {
            Logger.Warning(ex,"AddInflowData failed.");
        }
        finally
        {
           App.UpdateSemaphore.Release();
        }
        
    }
    private async Task AddOutFlowData(int days)
    {
        await App.UpdateSemaphore.WaitAsync();
        try
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

            var context = UpdateContext;
            var withdraws = await context.Mutations
                .AsNoTracking()
                .Where(x => x.Transaction.TimeStamp.Date >= startDate
                    // && x.Transaction.TimeStamp.Date <= endDate
                    && x.Type == Enums.TransactionKind.Withdraw)
                .Include(t => t.Transaction)
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
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "AddOutflowData failed.");
        }
        finally
        {
            App.UpdateSemaphore.Release();
        }
    }
    private async Task AddPortfolioValueData(List<AssetTotals> assets, int days)
    {
        //progressCounter = 0;
        //progressInterval = (100.0/assets.Count);

        _graphService.ClearHistoricalDataBuffer();

        foreach (var asset in assets)
        {
            if (cts is null || cts.IsCancellationRequested || IsPauseRequested)
            {
                return; //break in case GraphUpdateService has been canceled or paused
            }
            //in case of a pre-listing coin the actual Marketvalue is fixed as long as it is not listed yet.
            //This market value is already calculated and obtained in the above assetTotals query
            var getDataResult = await GetHistoricalPrices(asset);

            await getDataResult.Match(async s => 
            {
                var historicalData = await CalculateAndPopulateHistoricalDataById(asset, days, s);
                _graphService.AddHistoricalDataToBuffer(historicalData);
               // await _graphService.SaveHistoricalDataBufferToJson(_portfolioService.CurrentPortfolio.Signature);
            },
            err =>
            {
                return Task.CompletedTask;
            });
            
            if (getDataResult.IsFaulted)
            {
                continue;
            }
            //progressCounter++;

            //MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            //{
            //    _messenger.Send(new UpdateProgressValueMessage(Convert.ToInt16((100 * progressCounter / assets.Count))));
            //});
            
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
        if (dataSetChart == null || dataSetChart.Length == 0) { return data; }

        if (dataSetChart.Length < days)
        {
            days = dataSetChart.Length;
        }

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
            //var dates = dataByIds.First().DataPoints.Select(x => x.Date).ToList();
            var dates = dataByIds
                .MaxBy(x => x.DataPoints.Count)?
                .DataPoints
                .Select(x => x.Date)
                .ToList() ?? new List<DateOnly>();

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
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning("Historical Data {0} of {1} data points added", nrOfPointsAdded, dataByIds.First().DataPoints.Count);
            return false;
        }
        finally
        {
            await _graphService.SaveGraphToJson(PortfolioSignature);
           // _graphService.ClearHistoricalDataBuffer(_portfolioService.CurrentPortfolio.Signature);
            Logger.Information("Historical Data updated and saved");
        }
    }
    private async Task<Result<MarketChartById>> GetHistoricalPrices(AssetTotals asset)
    {
        var marketChart = new MarketChartById();

        var suffix = asset.Coin.Name.Contains("_pre-listing") ? "-prelisting" : "";

        var loadResult = await marketChart.LoadMarketChartJson(asset.Coin.ApiId + suffix);
        loadResult.IfLeft(err =>
        {
                Logger.Warning("Error loading MarketChart for {0}: " + err.Message, asset.Coin.ApiId);
            });
        
        return marketChart ?? new MarketChartById();
    }


    private async Task<double> GetHistoricalQtyByDate(DateOnly _date, AssetTotals asset)
    {
        await App.UpdateSemaphore.WaitAsync();
        try
        {
            var context = UpdateContext;
            var historicalQty = asset.Qty;
            var date = _date.ToDateTime(TimeOnly.Parse("01:00 AM"));

            var mutations = await context.Mutations
                .AsNoTracking()
                .Where(m => m.Asset.Coin.ApiId.ToLower() == asset.Coin.ApiId.ToLower() &&
                            m.Transaction.TimeStamp.Date >= date.Date &&
                            m.Transaction.TimeStamp.Date <= DateTime.UtcNow)
                .Include(m => m.Transaction)
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
        catch (Exception ex)
        {
            Logger.Warning(ex, $"GetHistoricalQtyByDate failed for {asset?.Coin.ApiId}");
            return 0;
        }
        finally
        {
           App.UpdateSemaphore.Release();
        }


       
    }
    private async Task<List<AssetTotals>> GetAssets()
    {
        await App.UpdateSemaphore.WaitAsync();
        try
        {
            var context = UpdateContext;
            var assets = await context.Assets
                .AsNoTracking()
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
        catch (Exception ex)
        {
            Logger.Warning(ex, $"GetAssets failed. Returned empty list");
            return new List<AssetTotals>();
        }
        finally
        {
           App.UpdateSemaphore.Release();
        }
    }

    private async Task<int> GetDaysBasedOnOldestMarketChartData()
    {
   
        try
        {
            if (!Directory.Exists(AppConstants.ChartsFolder))
            {
                Logger.Warning(
                    "MarketCharts folder does not exist: {ChartsFolder}",
                    AppConstants.ChartsFolder);

                return 0;
            }

            DateOnly oldestDate = DateOnly.MaxValue;

            IEnumerable<string> marketChartFiles = Directory.EnumerateFiles(
                AppConstants.ChartsFolder,
                "MarketChart_*.json",
                SearchOption.TopDirectoryOnly);

            foreach (string fileName in marketChartFiles)
            {
                try
                {
                    if (fileName.Contains("ExchangeRates", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    await using FileStream stream = File.OpenRead(fileName);

                    decimal?[][]? prices =
                        await System.Text.Json.JsonSerializer.DeserializeAsync<decimal?[][]>(stream);

                    if (prices is null)
                    {
                        continue;
                    }

                    foreach (decimal?[]? pricePoint in prices)
                    {
                        if (pricePoint is null ||
                            pricePoint.Length == 0 ||
                            !pricePoint[0].HasValue)
                        {
                            continue;
                        }
                        decimal timestamp = pricePoint[0].Value;

                        if (timestamp <= 0)
                        {
                            continue;
                        }

                        DateTime pointDate = DateTime.UnixEpoch
                            .AddMilliseconds((double)timestamp)
                            .Date;

                        DateOnly date = DateOnly.FromDateTime(pointDate);

                        if (date < oldestDate)
                        {
                            oldestDate = date;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // One corrupt market-chart file should not prevent the other
                    // market-chart files from being scanned.
                    Logger.Warning(
                        ex,
                        "Could not scan market-chart file {FileName}",
                        fileName);
                }
            }

            if (oldestDate == DateOnly.MaxValue)
            {
                return 0;
            }

            int days = DateTime.UtcNow.Date
                .Subtract(oldestDate.ToDateTime(TimeOnly.MinValue))
                .Days + 1;

            Logger.Information(
                "Oldest market-chart data point is {OldestDate}; using {Days} days",
                oldestDate,
                days);

            return Math.Max(days, 0);
        }
        catch (Exception ex)
        {
            Logger.Warning(
                ex,
                "GetDaysBasedOnOldestMarketChartData failed. Returned 0 days");

            return 0;
        }
    }
    private async Task<int> GetDaysBasedOnOldestTransaction()
    {
        await App.UpdateSemaphore.WaitAsync();
        try
        {
            var context = UpdateContext;
            
            var tx = await context.Transactions.AsNoTracking().MinAsync(x => x.TimeStamp.Date);

            var days = tx.IsDefault() ? DateTime.UtcNow.Subtract(tx).Days + 1 : 0;

            return days;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, $"GetDaysBasedOnOldestTransaction failed. Returned 0 days");
            return 0;
        }
        finally
        {
            App.UpdateSemaphore.Release();
        }

    }
    private async Task<DateTime> GetOldestTransaction()
    {
        await App.UpdateSemaphore.WaitAsync();
        try
        {
            var context = UpdateContext;
            
            var tx = await context.Transactions.AsNoTracking().MinAsync(x => x.TimeStamp.Date);
            return tx;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, $"GetOldestTransaction failed. Returned default");
            return DateTime.MinValue;
        }
        finally
        {
            App.UpdateSemaphore.Release();
        }

    }
    private int GetDaysBasedOnLatestDataPoint()
    {
        var lastDataPointDate = _graphService.GetLatestDataPointDate();
        var days = DateTime.UtcNow.Subtract(lastDataPointDate.ToDateTime(TimeOnly.Parse("01:00 AM"))).Days;

        return days;
    }
    
    
    private async void ObtainFirstPortfolioValue()
    {
        await App.UpdateSemaphore.WaitAsync();
        try
        {
            var context = UpdateContext;
            var beforeDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(365)).Date;

            var sumDeposits = context.Mutations
                .AsNoTracking()
                .Where(x => x.Type == Enums.TransactionKind.Deposit && x.Transaction.TimeStamp.Date < beforeDate)
                .Include(t => t.Transaction)
                .Sum(x => x.Qty * x.Price);

            var sumWithDraws = context.Mutations
                .AsNoTracking()
                .Where(x => x.Type == Enums.TransactionKind.Withdraw && x.Transaction.TimeStamp.Date < beforeDate)
                .Include(t => t.Transaction)
                .Sum(x => x.Qty * x.Price);

            _graphService.AddDataPointInFlow(new DataPoint { Date = DateOnly.FromDateTime(beforeDate), Value = sumDeposits });
            _graphService.AddDataPointOutFlow(new DataPoint { Date = DateOnly.FromDateTime(beforeDate), Value = sumWithDraws });
            _graphService.AddDataPointPortfolio(new DataPoint { Date = DateOnly.FromDateTime(beforeDate), Value = sumDeposits - sumWithDraws });

        }
        catch (Exception ex)
        {
            Logger.Warning(ex, $"ObtainFirstPortfolioValue failed.");
        }
        finally
        {
            App.UpdateSemaphore.Release();
        }
        
    }


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}

