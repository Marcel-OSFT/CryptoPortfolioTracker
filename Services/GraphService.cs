
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using LiveChartsCore.Defaults;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IGraphService
{
    bool IsLoadingFromJson { get; set; }

    public Task LoadGraphFromJson(string portfolioSignature);
    public Task SaveGraphToJson(string portfolioSignature);
    public Task RegisterModification(Transaction transactionA, Transaction? transactionB = null);
    public Task ApplyModification(string portfolioSignature);
    public ObservableCollection<DateTimePoint> GetPortfolioValues();
    public ObservableCollection<DateTimePoint> GetInFlowValues();
    public ObservableCollection<DateTimePoint> GetOutFlowValues();
    public bool IsModificationRequested();
    public DateOnly GetModifyFromDate();
    public void ClearHistoricalDataBuffer();
    public void AddDataPointInFlow(DataPoint dataPoint);
    public void AddDataPointPortfolio(DataPoint dataPoint);
    public void AddDataPointOutFlow(DataPoint dataPoint);
    public void AddHistoricalDataToBuffer(HistoricalDataByIdRev historicalData);
    List<HistoricalDataByIdRev> GetHistoricalDataBuffer();
    DateOnly GetLatestDataPointDate();
    bool HasDataPoints();
    int GetHistoricalDataBufferDatesCount();
    DateOnly GetHistoricalDataBufferLatestDate();
    bool HasHistoricalDataBuffer();
    DataPoint GetLatestDataPointInFlow();
    DataPoint GetLatestDataPointOutFlow();
    string GetSignature();
}

public partial class GraphService : ObservableObject, IGraphService
{
    private readonly IAppState _appState;

    private string Currency => _appState.Currency;
    private Graph PortfolioGraph { get; set; } = new();
    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphService).Name.PadRight(22));

    private readonly IExchangeRateService _exchangeRateService;

    private List<HistoricalDataByIdRev> HistoricalDataByIdsBufferList { get; set; } = new();

    [ObservableProperty] private bool isLoadingFromJson;
    
    public GraphService(IExchangeRateService exchangeRateService, IAppState appState) 
    {
        _appState = appState;
        _appState.CurrencyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Currency));
        };
        _exchangeRateService = exchangeRateService;
        HistoricalDataByIdsBufferList = new();
    }

    public async Task LoadGraphFromJson(string portfolioSignature)
    {
        try
        {
            PortfolioGraph = new();
            IsLoadingFromJson = true;
            var graphPath = Path.Combine(AppConstants.PortfoliosPath, portfolioSignature);

            Directory.CreateDirectory(graphPath); // if not exists
            await LoadGraphDataAsync(Path.Combine(graphPath, "graph.json"), async stream =>
            {
                PortfolioGraph = await JsonSerializer.DeserializeAsync<Graph>(stream);
                Logger.Information("Graph data de-serialized successfully ({0} data points)", PortfolioGraph.DataPointsPortfolio.Count);
            });


            HistoricalDataByIdsBufferList = new();

            if (PortfolioGraph == null)
            {
                PortfolioGraph = new();
            }
            else
            {
                await CleanUpGraph(portfolioSignature);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to de-serialize Graph data");
            var restoreResult = await RestoreAndLoadBackupGraph(portfolioSignature);
            restoreResult.IfFail(ex => PortfolioGraph = new());
        }
        finally
        {
            IsLoadingFromJson = false;
        }
    }

    private static async Task LoadGraphDataAsync(string fileName, Func<FileStream, Task> processStream)
    {
        if (File.Exists(fileName))
        {
            try
            {
                using FileStream openStream = File.OpenRead(fileName);
                await processStream(openStream);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to de-serialize data from {0}", fileName);
            }
        }
    }

    private async Task<Result<bool>> RestoreAndLoadBackupGraph(string portfolioSignature)
    {
        var graphPath = Path.Combine(AppConstants.PortfoliosPath, portfolioSignature);
        var backupFileName = Path.Combine(graphPath, "graph.json.bak");
        if (File.Exists(backupFileName))
        {
            try
            {
                File.Copy(backupFileName, Path.Combine(graphPath, "graph.json"), true);
                Logger.Information("Restored backup graph.json file");
                await LoadGraphDataAsync(Path.Combine(graphPath, "graph.json"), async stream =>
                {
                    PortfolioGraph = await JsonSerializer.DeserializeAsync<Graph>(stream);
                    Logger.Information("Graph data de-serialized successfully ({0} data points)", PortfolioGraph.DataPointsPortfolio.Count);
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Restoring and de-serializing backup graph failed");
                return new Result<bool>(false);
            }
            return new Result<bool>(true); ;
        }
        Logger.Error("No backup graph available");
        return new Result<bool>(false);
    }
    
   public async Task SaveGraphToJson(string portfolioSignature)
    {
        try
        {
            var fileName = Path.Combine(AppConstants.PortfoliosPath, portfolioSignature, "graph.json");
            var backupFileName = fileName + ".bak";

            if (File.Exists(fileName))
            {
                File.Copy(fileName, backupFileName, true);
            }

            await using FileStream createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, PortfolioGraph);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to serialize Graph data");
        }
    }

   
    private async Task CleanUpGraph(string portfolioSignature)
    {
        // Remove duplicate entries
        PortfolioGraph.DataPointsInFlow = RemoveDuplicates(PortfolioGraph.DataPointsInFlow);
        PortfolioGraph.DataPointsOutFlow = RemoveDuplicates(PortfolioGraph.DataPointsOutFlow);
        PortfolioGraph.DataPointsPortfolio = RemoveDuplicates(PortfolioGraph.DataPointsPortfolio);

        // Remove leading zeros
        var firstValuePoint = PortfolioGraph.DataPointsPortfolio.FirstOrDefault(x => x.ValueUsd > 0);

        if (firstValuePoint != null)
        {
            var date = firstValuePoint.Date;
            var sumInFlow = PortfolioGraph.DataPointsInFlow.Where(x => x.Date < date).Sum(x => x.ValueUsd);
            var sumOutFlow = PortfolioGraph.DataPointsOutFlow.Where(x => x.Date < date).Sum(x => x.ValueUsd);

            PortfolioGraph.DataPointsPortfolio = RemoveLeadingZeros(PortfolioGraph.DataPointsPortfolio, date, sumInFlow - sumOutFlow);
            PortfolioGraph.DataPointsInFlow = RemoveLeadingZeros(PortfolioGraph.DataPointsInFlow, date, sumInFlow);
            PortfolioGraph.DataPointsOutFlow = RemoveLeadingZeros(PortfolioGraph.DataPointsOutFlow, date, sumOutFlow);

            await SaveGraphToJson(portfolioSignature);
        }
    }

    private static List<DataPoint> RemoveDuplicates(List<DataPoint> dataPoints)
    {
        return dataPoints
            .GroupBy(x => new { x.Date, x.ValueUsd })
            .Select(g => g.First())
            .ToList();
    }

    private List<DataPoint> RemoveLeadingZeros(List<DataPoint> dataPoints, DateOnly date, double initialValue)
    {
        var filteredDataPoints = dataPoints.Where(x => x.Date >= date).ToList();
        filteredDataPoints.Add(new DataPoint { Date = date.AddDays(-1), ValueUsd = initialValue, ValueEur = initialValue / (_exchangeRateService.TryGetDailyRate(date, out var rate) ? rate : 1) });
        return filteredDataPoints.OrderBy(x => x.Date).ToList();
    }

    public void ClearHistoricalDataBuffer()
    {
        HistoricalDataByIdsBufferList = new();
    }

    public async Task RegisterModification(Transaction transactionA, Transaction? transactionB = null)
    {
        try
        {
            DateOnly modDate = PortfolioGraph.ModifyFromDate;

            while (PortfolioGraph.GraphStatus == GraphStatus.Modifying)
            {
                await Task.Delay(1000);
            }

            var dateA = DateOnly.FromDateTime(transactionA.TimeStamp);
            var dateB = transactionB is not null
                ? DateOnly.FromDateTime(transactionB.TimeStamp)
                : DateOnly.FromDateTime(DateTime.Now);

            var earliestDate = dateB.CompareTo(dateA) <= 0 ? dateB : dateA;

            if (modDate.Equals(DateOnly.MinValue))
            {
                modDate = earliestDate;
            }
            else
            {
                modDate = earliestDate.CompareTo(modDate) <= 0 ? earliestDate : modDate;
            }

            PortfolioGraph.ModifyFromDate = modDate;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to register modification");
        }
    }

    public async Task ApplyModification(string portfolioSignature)
    {
        try
        {
            PortfolioGraph.GraphStatus = GraphStatus.Modifying;
            var modDate = PortfolioGraph.ModifyFromDate;

            RemoveDataPointsFromDate(PortfolioGraph.DataPointsPortfolio, modDate);
            RemoveDataPointsFromDate(PortfolioGraph.DataPointsInFlow, modDate);
            RemoveDataPointsFromDate(PortfolioGraph.DataPointsOutFlow, modDate);

            await SaveGraphToJson(portfolioSignature);

            PortfolioGraph.ModifyFromDate = DateOnly.MinValue;
            PortfolioGraph.IsModificationRequested = false;
            PortfolioGraph.GraphStatus = GraphStatus.Idle;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to apply modification");
        }
    }

    private static void RemoveDataPointsFromDate(List<DataPoint> dataPoints, DateOnly date)
    {
        dataPoints.RemoveAll(x => x.Date >= date);
    }


    public ObservableCollection<DateTimePoint> GetPortfolioValues()
    {
        var values = new ObservableCollection<DateTimePoint>();

        if (PortfolioGraph == null) return values;

        foreach (var dataPoint in PortfolioGraph.DataPointsPortfolio)
        {
            var dateTimePoint = new DateTimePoint(dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")), dataPoint.GetValue(_appState.Currency));
            values.Add(dateTimePoint);
        }
        return values;
    }
    
    public ObservableCollection<DateTimePoint> GetInFlowValues()
    {
        var values = new ObservableCollection<DateTimePoint>();
        if (PortfolioGraph == null) return values;

        foreach (var dataPoint in PortfolioGraph.DataPointsInFlow)
        {
            var dateTimePoint = new DateTimePoint(dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")), dataPoint.GetValue(_appState.Currency));
            values.Add(dateTimePoint);
        }
        return values;
    }
    
    public ObservableCollection<DateTimePoint> GetOutFlowValues()
    {
        var values = new ObservableCollection<DateTimePoint>();
        if (PortfolioGraph is null) return values;

        foreach (var dataPoint in PortfolioGraph.DataPointsOutFlow)
        {
            var dateTimePoint = new DateTimePoint(dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")), dataPoint.GetValue(_appState.Currency));
            values.Add(dateTimePoint);
        }
        return values;
    }

    public bool IsModificationRequested()
    {
        return PortfolioGraph?.IsModificationRequested ?? false;
    }

    public string GetSignature()
    {
        return PortfolioGraph != null ? PortfolioGraph.Signature : string.Empty;
    }

    public DateOnly GetModifyFromDate()
    {
        return PortfolioGraph?.ModifyFromDate ?? DateOnly.MinValue;
    }

    public void AddDataPointInFlow(DataPoint dataPoint)
    {
        if (dataPoint != null)
        {
            PortfolioGraph.DataPointsInFlow.Add(dataPoint);
        }
    }
    public void AddDataPointOutFlow(DataPoint dataPoint)
    {
        if (dataPoint != null)
        {
            PortfolioGraph.DataPointsOutFlow.Add(dataPoint);
        }
    }
    public void AddDataPointPortfolio(DataPoint dataPoint)
    {
        if (dataPoint != null)
        {
            PortfolioGraph.DataPointsPortfolio.Add(dataPoint);
        }
    }

    public void AddHistoricalDataToBuffer(HistoricalDataByIdRev historicalData)
    {
        if (historicalData?.DataPoints?.Count > 0)
        {
            HistoricalDataByIdsBufferList.Add(historicalData);
        }
    }

    public List<HistoricalDataByIdRev> GetHistoricalDataBuffer()
    {
        return HistoricalDataByIdsBufferList;
    }

    public DateOnly GetLatestDataPointDate()
    {
        return PortfolioGraph.DataPointsPortfolio.MaxBy(x => x.Date)?.Date ?? DateOnly.MinValue;
    }

    public DataPoint GetLatestDataPointInFlow()
    {
        return PortfolioGraph.DataPointsInFlow.MaxBy(x => x.Date);
    }
    public DataPoint GetLatestDataPointOutFlow()
    {
        return PortfolioGraph.DataPointsOutFlow.MaxBy(x => x.Date);
    }
    public bool HasDataPoints()
    {
        return PortfolioGraph?.DataPointsPortfolio.Count > 0;
    }
    public bool HasHistoricalDataBuffer()
    {
        return HistoricalDataByIdsBufferList.Count > 0;
    }

    public int GetHistoricalDataBufferDatesCount()
    {
        return HistoricalDataByIdsBufferList.FirstOrDefault()?.DataPoints.Count ?? 0;
    }

    public DateOnly GetHistoricalDataBufferLatestDate()
    {
        return HistoricalDataByIdsBufferList.FirstOrDefault()?.DataPoints.MaxBy(x => x.Date)?.Date ?? DateOnly.MinValue;
    }

    public DateOnly GetHistoricalDataBufferOldestDate()
    {
        return HistoricalDataByIdsBufferList.FirstOrDefault()?.DataPoints.MinBy(x => x.Date)?.Date ?? DateOnly.MinValue;
    }

    
}