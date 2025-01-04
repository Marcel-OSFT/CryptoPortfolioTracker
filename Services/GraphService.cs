
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

[ObservableObject]
public partial class GraphService : IGraphService
{
    private Graph graph;
    private static ILogger Logger { get; set; }
    private List<HistoricalDataByIdRev> HistoricalDataByIdsBufferList { get; set; } = new();
    [ObservableProperty] private bool isLoadingFromJson;
    
    private readonly string chartsFolder = Path.Combine(App.appDataPath, App.ChartsFolder);

    public GraphService() 
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphService).Name.PadRight(22));
        HistoricalDataByIdsBufferList = new();
    }

    //public async Task LoadGraphFromJsonOld()
    //{
    //    try
    //    {
    //        IsLoadingFromJson = true;

    //        if (!Directory.Exists(chartsFolder))
    //        {
    //            Directory.CreateDirectory(chartsFolder);
    //        }
    //        var fileName = chartsFolder + "\\graph.json";
    //        if (File.Exists(fileName))
    //        {
    //            using FileStream openStream = File.OpenRead(fileName);
    //            graph = await JsonSerializer.DeserializeAsync<Graph>(openStream);
    //            Logger.Information("Graph data de-serialized succesfully ({0} data points)", graph.DataPointsPortfolio.Count);
    //        }

    //        fileName = chartsFolder + "\\HistoryBuffer.json";
    //        if (File.Exists(fileName))
    //        {
    //            try
    //            {
    //                using FileStream openStream = File.OpenRead(fileName);
    //                HistoricalDataByIdsBufferList = await JsonSerializer.DeserializeAsync<List<HistoricalDataByIdRev>>(openStream);

    //                openStream.Close();


    //                if (HistoricalDataByIdsBufferList is not null && HistoricalDataByIdsBufferList.Count > 0)
    //                {
    //                    CheckValidityHistoricalDataBuffer();
    //                }

    //            }
    //            catch (Exception ex)
    //            {
    //                Logger.Error(ex, "Failed to de-serialize HistoricalDataByIdRev data");
    //            }
    //        }

    //        if (graph is null)
    //        {
    //            graph = new();

    //        }
    //        else //**** check existing graph for 'voorloper nullen' and correct
    //        {
    //            await CleanUpGraph();

    //        }

    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.Error(ex, "Failed to de-serialize Graph data");

    //        //*** check if backup file exists and restore it
    //        var backupFileName = chartsFolder + "\\graph.json.bak";
    //        if (File.Exists(backupFileName))
    //        {
    //            File.Copy(backupFileName, chartsFolder + "\\graph.json", true);
    //            Logger.Information("Restored backup graph.json file");
    //        }
    //    }
    //    finally
    //    {
    //        IsLoadingFromJson = false;
    //    }
    //}
    public async Task LoadGraphFromJson()
    {
        try
        {
            IsLoadingFromJson = true;
            
            Directory.CreateDirectory(chartsFolder);
            await LoadGraphDataAsync(chartsFolder + "\\graph.json", async stream =>
            {
                graph = await JsonSerializer.DeserializeAsync<Graph>(stream);
                Logger.Information("Graph data de-serialized successfully ({0} data points)", graph.DataPointsPortfolio.Count);
            });

            await LoadGraphDataAsync(chartsFolder + "\\HistoryBuffer.json", async stream =>
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
                await CleanUpGraph();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to de-serialize Graph data");
            RestoreBackupGraph();
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

    private void RestoreBackupGraph()
    {
        var backupFileName = chartsFolder + "\\graph.json.bak";
        if (File.Exists(backupFileName))
        {
            File.Copy(backupFileName, chartsFolder + "\\graph.json", true);
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


    //private async Task CleanUpGraph()
    //{
    //    //*** First remove (possible) duplicate entries
    //    var inFlow = graph.DataPointsInFlow = graph.DataPointsInFlow
    //        .GroupBy(x => new { x.Date, x.Value })
    //        .Select(g => g.First())
    //        .ToList();

    //    var outFlow = graph.DataPointsOutFlow = graph.DataPointsOutFlow
    //        .GroupBy(x => new { x.Date, x.Value })
    //        .Select(g => g.First())
    //        .ToList();

    //    var portfolio = graph.DataPointsPortfolio = graph.DataPointsPortfolio
    //        .GroupBy(x => new { x.Date, x.Value })
    //        .Select(g => g.First())
    //        .ToList();


    //    //*** Second, remove possible leading zeros

    //    var firstValuePoint = portfolio.Where(x => x.Value > 0).OrderBy(x => x.Date).FirstOrDefault();

    //    if (firstValuePoint != null)
    //    {
    //        var date = firstValuePoint.Date;
    //        var sumInFlow = inFlow.Where(x => x.Date < firstValuePoint.Date).Sum(x => x.Value);
    //        var sumOutFlow = outFlow.Where(x => x.Date < firstValuePoint.Date).Sum(x => x.Value);

    //        portfolio = portfolio.Where(x => x.Date >= date).ToList();
    //        inFlow = inFlow.Where(x => x.Date >= date).ToList();
    //        outFlow = outFlow.Where(x => x.Date >= date).ToList();

    //        var point = new DataPoint() { Date = date.AddDays(-1), Value = sumInFlow - sumOutFlow };
    //        portfolio.Add(point);

    //        point = new DataPoint() { Date = date.AddDays(-1), Value = sumInFlow };
    //        inFlow.Add(point);

    //        point = new DataPoint() { Date = date.AddDays(-1), Value = sumOutFlow };
    //        outFlow.Add(point);

    //        graph.DataPointsPortfolio = portfolio.OrderBy(x => x.Date).ToList();
    //        graph.DataPointsInFlow = inFlow.OrderBy(x => x.Date).ToList();
    //        graph.DataPointsOutFlow = outFlow.OrderBy(x => x.Date).ToList();

    //        await SaveGraphToJson();
    //    }
    //}

    private async Task CleanUpGraph()
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

            await SaveGraphToJson();
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
    public async Task SaveGraphToJson()
    {
        try
        {
            var backupFileName = Path.Combine(chartsFolder, "graph.json.bak");
            var fileName = Path.Combine(chartsFolder, "graph.json");

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
            var fileName = Path.Combine(chartsFolder, "HistoryBuffer.json");
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

        var historyBufferFile = Path.Combine(chartsFolder, "HistoryBuffer.json");
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

    //public async Task ApplyModification()
    //{
    //    try
    //    {
    //        graph.GraphStatus = GraphStatus.Modifying;
    //        var modDate = graph.ModifyFromDate;

    //        graph.DataPointsPortfolio.RemoveAll(x => x.Date >= modDate);
    //        graph.DataPointsInFlow.RemoveAll(x => x.Date >= modDate);
    //        graph.DataPointsOutFlow.RemoveAll(x => x.Date >= modDate);

    //        //*** might be a good idea to Save the modified Graph here
    //        //*** In case of a crash or when the application is shutdown before the New Graph has been recalculated and saved
    //        await SaveGraphToJson();

    //        graph.ModifyFromDate = DateOnly.MinValue;
    //        graph.IsModificationRequested = false;
    //        graph.GraphStatus = GraphStatus.Idle;
    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.Error(ex, "Failed to apply modification");
    //    }
    //}
    public async Task ApplyModification()
    {
        try
        {
            graph.GraphStatus = GraphStatus.Modifying;
            var modDate = graph.ModifyFromDate;

            RemoveDataPointsFromDate(graph.DataPointsPortfolio, modDate);
            RemoveDataPointsFromDate(graph.DataPointsInFlow, modDate);
            RemoveDataPointsFromDate(graph.DataPointsOutFlow, modDate);

            await SaveGraphToJson();

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

    //public Graph GetGraph()
    //{
    //    return graph;
    //}

    //public async Task SetGraph(Graph graph)
    //{
    //    this.graph = graph;
    //    await SaveGraphToJson();
        
    //}

    //public async Task<Graph> GetGraphFromJson()
    //{
    //    Graph _graph = new();
    //    try
    //    {
    //        IsLoadingFromJson = true;

    //        if (!Directory.Exists(chartsFolder))
    //        {
    //            Directory.CreateDirectory(chartsFolder);
    //        }
    //        var fileName = chartsFolder + "\\graph.json";
    //        if (File.Exists(fileName))
    //        {
    //            using FileStream openStream = File.OpenRead(fileName);
    //            _graph = await JsonSerializer.DeserializeAsync<Graph>(openStream);
    //            Logger.Information("Graph data de-serialized succesfully ({0} data points)", _graph.DataPointsPortfolio.Count);
    //        }

    //        fileName = chartsFolder + "\\HistoryBuffer.json";
    //        if (File.Exists(fileName))
    //        {
    //            using FileStream openStream = File.OpenRead(fileName);
    //            HistoricalDataByIdsBufferList = await JsonSerializer.DeserializeAsync<List<HistoricalDataByIdRev>>(openStream);
    //        }

    //    }
    //    catch (Exception ex)
    //    {
    //        Logger.Error(ex, "Failed to de-serialize HGraph data");
    //    }
    //    finally
    //    {
    //        IsLoadingFromJson = false;
    //    }
    //    return _graph;
    //}



}