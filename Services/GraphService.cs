
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
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

public partial class GraphService : ObservableObject, IGraphService
{
    private Graph graph = new();
    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphService).Name.PadRight(22));
    private List<HistoricalDataByIdRev> HistoricalDataByIdsBufferList { get; set; } = new();
    [ObservableProperty] private bool isLoadingFromJson;
    
    //private readonly string chartsFolder = Path.Combine(App.appDataPath, App.ChartsFolder);

    public GraphService() 
    {
        HistoricalDataByIdsBufferList = new();
    }

    public async Task LoadGraphFromJson(string portfolioPath)
    {
        try
        {
            IsLoadingFromJson = true;
            var graphPath = Path.Combine(App.appDataPath, portfolioPath);

            Directory.CreateDirectory(graphPath); // if not exists
            await LoadGraphDataAsync(Path.Combine(graphPath, "graph.json"), async stream =>
            {
                graph = await JsonSerializer.DeserializeAsync<Graph>(stream);
                Logger.Information("Graph data de-serialized successfully ({0} data points)", graph.DataPointsPortfolio.Count);
            });

            await LoadGraphDataAsync(Path.Combine(graphPath, "HistoryBuffer.json"), async stream =>
            {
                HistoricalDataByIdsBufferList = await JsonSerializer.DeserializeAsync<List<HistoricalDataByIdRev>>(stream);
                if (HistoricalDataByIdsBufferList?.Count > 0)
                {
                    CheckValidityHistoricalDataBuffer();
                }
            });

            if (graph == null)
            {
                graph = new();
            }
            else
            {
                await CleanUpGraph(portfolioPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to de-serialize Graph data");
            RestoreBackupGraph(portfolioPath);
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

    private void RestoreBackupGraph(string portfolioPath)
    {
        var graphPath = Path.Combine(App.appDataPath, portfolioPath);
        var backupFileName = Path.Combine(graphPath, "graph.json.bak");
        if (File.Exists(backupFileName))
        {
            File.Copy(backupFileName, Path.Combine(graphPath, "graph.json"), true);
            Logger.Information("Restored backup graph.json file");
        }
    }
    private void CheckValidityHistoricalDataBuffer()
    {
        if (HistoricalDataByIdsBufferList?.Count > 0 && graph?.DataPointsPortfolio?.Count > 0)
        {
            if (GetHistoricalDataBufferOldestDate() != GetLatestDataPointDate().AddDays(1))
            {
                ClearHistoricalDataBuffer();
            }
        }
    }


    private async Task CleanUpGraph(string portfolioPath)
    {
        // Remove duplicate entries
        graph.DataPointsInFlow = RemoveDuplicates(graph.DataPointsInFlow);
        graph.DataPointsOutFlow = RemoveDuplicates(graph.DataPointsOutFlow);
        graph.DataPointsPortfolio = RemoveDuplicates(graph.DataPointsPortfolio);

        // Remove leading zeros
        var firstValuePoint = graph.DataPointsPortfolio.FirstOrDefault(x => x.Value > 0);

        if (firstValuePoint != null)
        {
            var date = firstValuePoint.Date;
            var sumInFlow = graph.DataPointsInFlow.Where(x => x.Date < date).Sum(x => x.Value);
            var sumOutFlow = graph.DataPointsOutFlow.Where(x => x.Date < date).Sum(x => x.Value);

            graph.DataPointsPortfolio = RemoveLeadingZeros(graph.DataPointsPortfolio, date, sumInFlow - sumOutFlow);
            graph.DataPointsInFlow = RemoveLeadingZeros(graph.DataPointsInFlow, date, sumInFlow);
            graph.DataPointsOutFlow = RemoveLeadingZeros(graph.DataPointsOutFlow, date, sumOutFlow);

            await SaveGraphToJson(portfolioPath);
        }
    }

    private static List<DataPoint> RemoveDuplicates(List<DataPoint> dataPoints)
    {
        return dataPoints
            .GroupBy(x => new { x.Date, x.Value })
            .Select(g => g.First())
            .ToList();
    }

    private static List<DataPoint> RemoveLeadingZeros(List<DataPoint> dataPoints, DateOnly date, double initialValue)
    {
        var filteredDataPoints = dataPoints.Where(x => x.Date >= date).ToList();
        filteredDataPoints.Add(new DataPoint { Date = date.AddDays(-1), Value = initialValue });
        return filteredDataPoints.OrderBy(x => x.Date).ToList();
    }
    public async Task SaveGraphToJson(string portfolioPath)
    {
        try
        {
            var graphPath = Path.Combine(App.appDataPath, portfolioPath);
            var backupFileName = Path.Combine(graphPath, "graph.json.bak");
            var fileName = Path.Combine(graphPath, "graph.json");

            if (File.Exists(fileName))
            {
                File.Copy(fileName, backupFileName, true);
            }

            await using FileStream createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, graph);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to serialize Graph data");
        }
    }

    public async Task SaveHistoricalDataBufferToJson()
    {
        if (HistoricalDataByIdsBufferList.Count > 0)
        {
            var chartsPath = Path.Combine(App.appDataPath, App.ChartsFolder);
            var fileName = Path.Combine(chartsPath, "HistoryBuffer.json");
            try
            {
                await using FileStream createStream = File.Create(fileName);
                await JsonSerializer.SerializeAsync(createStream, HistoricalDataByIdsBufferList);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to serialize HistoricalDataBuffer data");
            }
        }
    }

    public void ClearHistoricalDataBuffer()
    {
        HistoricalDataByIdsBufferList.Clear();

        var chartsPath = Path.Combine(App.appDataPath, App.ChartsFolder);
        var historyBufferFile = Path.Combine(chartsPath, "HistoryBuffer.json");
        if (File.Exists(historyBufferFile))
        {
            File.Delete(historyBufferFile);
        }
    }

    public async Task RegisterModification(Transaction transactionA, Transaction? transactionB = null)
    {
        try
        {
            DateOnly modDate = graph.ModifyFromDate;

            while (graph.GraphStatus == GraphStatus.Modifying)
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

            graph.ModifyFromDate = modDate;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to register modification");
        }
    }

    public async Task ApplyModification(string portfolioPath)
    {
        try
        {
            graph.GraphStatus = GraphStatus.Modifying;
            var modDate = graph.ModifyFromDate;

            RemoveDataPointsFromDate(graph.DataPointsPortfolio, modDate);
            RemoveDataPointsFromDate(graph.DataPointsInFlow, modDate);
            RemoveDataPointsFromDate(graph.DataPointsOutFlow, modDate);

            await SaveGraphToJson(portfolioPath);

            graph.ModifyFromDate = DateOnly.MinValue;
            graph.IsModificationRequested = false;
            graph.GraphStatus = GraphStatus.Idle;
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

        if (graph == null) return values;

        foreach (var dataPoint in graph.DataPointsPortfolio)
        {
            var dateTimePoint = new DateTimePoint(dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")), dataPoint.Value);
            values.Add(dateTimePoint);
        }
        return values;
    }
    
    public ObservableCollection<DateTimePoint> GetInFlowValues()
    {
        var values = new ObservableCollection<DateTimePoint>();
        if (graph == null) return values;

        foreach (var dataPoint in graph.DataPointsInFlow)
        {
            var dateTimePoint = new DateTimePoint(dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")), dataPoint.Value);
            values.Add(dateTimePoint);
        }
        return values;
    }
    
    public ObservableCollection<DateTimePoint> GetOutFlowValues()
    {
        var values = new ObservableCollection<DateTimePoint>();
        if (graph is null) return values;

        foreach (var dataPoint in graph.DataPointsOutFlow)
        {
            var dateTimePoint = new DateTimePoint(dataPoint.Date.ToDateTime(TimeOnly.Parse("01:00 AM")), dataPoint.Value);
            values.Add(dateTimePoint);
        }
        return values;
    }

    public bool IsModificationRequested()
    {
        return graph?.IsModificationRequested ?? false;
    }

    public DateOnly GetModifyFromDate()
    {
        return graph?.ModifyFromDate ?? DateOnly.MinValue;
    }

    public void AddDataPointInFlow(DataPoint dataPoint)
    {
        if (dataPoint != null)
        {
            graph.DataPointsInFlow.Add(dataPoint);
        }
    }
    public void AddDataPointOutFlow(DataPoint dataPoint)
    {
        if (dataPoint != null)
        {
            graph.DataPointsOutFlow.Add(dataPoint);
        }
    }
    public void AddDataPointPortfolio(DataPoint dataPoint)
    {
        if (dataPoint != null)
        {
            graph.DataPointsPortfolio.Add(dataPoint);
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
        return graph.DataPointsPortfolio.MaxBy(x => x.Date)?.Date ?? DateOnly.MinValue;
    }

    public DataPoint GetLatestDataPointInFlow()
    {
        return graph.DataPointsInFlow.MaxBy(x => x.Date);
    }
    public DataPoint GetLatestDataPointOutFlow()
    {
        return graph.DataPointsOutFlow.MaxBy(x => x.Date);
    }
    public bool HasDataPoints()
    {
        return graph?.DataPointsPortfolio.Count > 0;
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