
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
    private List<HistoricalDataByIdRev> HistoricalDataByIdsBufferList { get; set; }
    [ObservableProperty] bool isLoadingFromJson;
    
    string chartsFolder = App.appDataPath + "\\" + App.ChartsFolder;



    public GraphService() 
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(GraphService).Name.PadRight(22));
        HistoricalDataByIdsBufferList = new();
    }

    public async Task LoadGraphFromJson()
    {
        try
        {
            IsLoadingFromJson = true;

            if (!Directory.Exists(chartsFolder))
            {
                Directory.CreateDirectory(chartsFolder);
            }
            var fileName = chartsFolder + "\\graph.json";
            if (File.Exists(fileName))
            {
                using FileStream openStream = File.OpenRead(fileName);
                graph = await JsonSerializer.DeserializeAsync<Graph>(openStream);
                Logger.Information("Graph data de-serialized succesfully ({0} data points)", graph.DataPointsPortfolio.Count);
            }

            fileName = chartsFolder + "\\HistoryBuffer.json";
            if (File.Exists(fileName))
            {
                try
                {
                    using FileStream openStream = File.OpenRead(fileName);
                    HistoricalDataByIdsBufferList = await JsonSerializer.DeserializeAsync<List<HistoricalDataByIdRev>>(openStream);
                
                    if (HistoricalDataByIdsBufferList is not null && HistoricalDataByIdsBufferList.Count > 0)
                    {
                        CheckValidityHistoricalDataBuffer();
                    }
                
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to de-serialize HistoricalDataByIdRev data");
                }
            }

            if (graph is null)
            {
                graph = new();

            }
            else //**** check existing graph for 'voorloper nullen' and correct
            {
                await CleanUpGraph();
                
            }

        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to de-serialize HGraph data");
        }
        finally
        {
            IsLoadingFromJson = false;
        }

    }

    private void CheckValidityHistoricalDataBuffer()
    {
        if (HistoricalDataByIdsBufferList is not null && graph?.DataPointsPortfolio?.Count > 0)
        {
            if (GetHistoricalDataBufferOldestDate() != GetLatestDataPointDate().AddDays(1))
            {
                ClearHistoricalDataBuffer();
            }
        }
    }


    private async Task CleanUpGraph()
    {
        //*** First remove (possible) duplicate entries
        var inFlow = graph.DataPointsInFlow = graph.DataPointsInFlow
            .GroupBy(x => new { x.Date, x.Value })
            .Select(g => g.First())
            .ToList();

        var outFlow = graph.DataPointsOutFlow = graph.DataPointsOutFlow
            .GroupBy(x => new { x.Date, x.Value })
            .Select(g => g.First())
            .ToList();

        var portfolio = graph.DataPointsPortfolio = graph.DataPointsPortfolio
            .GroupBy(x => new { x.Date, x.Value })
            .Select(g => g.First())
            .ToList();


        //*** Second, remove possible leading zeros

        var firstValuePoint = portfolio.Where(x => x.Value > 0).OrderBy(x => x.Date).FirstOrDefault();

        if (firstValuePoint != null)
        {
            var date = firstValuePoint.Date;
            var sumInFlow = inFlow.Where(x => x.Date < firstValuePoint.Date).Sum(x => x.Value);
            var sumOutFlow = outFlow.Where(x => x.Date < firstValuePoint.Date).Sum(x => x.Value);

            portfolio = portfolio.Where(x => x.Date >= date).ToList();
            inFlow = inFlow.Where(x => x.Date >= date).ToList();
            outFlow = outFlow.Where(x => x.Date >= date).ToList();

            var point = new DataPoint() { Date = date.AddDays(-1), Value = sumInFlow - sumOutFlow };
            portfolio.Add(point);

            point = new DataPoint() { Date = date.AddDays(-1), Value = sumInFlow };
            inFlow.Add(point);

            point = new DataPoint() { Date = date.AddDays(-1), Value = sumOutFlow };
            outFlow.Add(point);

            graph.DataPointsPortfolio = portfolio.OrderBy(x => x.Date).ToList();
            graph.DataPointsInFlow = inFlow.OrderBy(x => x.Date).ToList();
            graph.DataPointsOutFlow = outFlow.OrderBy(x => x.Date).ToList();

            await SaveGraphToJson();
        }
    }

    public async Task SaveGraphToJson()
    {
        
        var fileName = chartsFolder + "\\graph.json";
        await using FileStream createStream = File.Create(fileName);
        await JsonSerializer.SerializeAsync(createStream, graph);
    }

    public async Task SaveHistoricalDataBufferToJson()
    {
        if (HistoricalDataByIdsBufferList.Count > 0)
        {
            var fileName = chartsFolder + "\\HistoryBuffer.json";
            await using FileStream createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, HistoricalDataByIdsBufferList);
        }
    }

    public void ClearHistoricalDataBuffer()
    {
        HistoricalDataByIdsBufferList.Clear();

        if (System.IO.File.Exists(chartsFolder + "\\HistoryBuffer.json"))
        {
            System.IO.File.Delete(chartsFolder + "\\HistoryBuffer.json");
        }
    }

    public async Task RegisterModification(Transaction transactionA, Transaction? transactionB = null)
    {
        DateOnly modDate = graph.ModifyFromDate;

        while (graph.GraphStatus == GraphStatus.Modifying)
        {
            await Task.Delay(1000);
        }

        var dateB = transactionB is not null
            ? DateOnly.FromDateTime(transactionB.TimeStamp)
            : DateOnly.FromDateTime(DateTime.Now);
        var dateA = DateOnly.FromDateTime(transactionA.TimeStamp);

        if (modDate.Equals(DateOnly.MinValue))
        {
            //assign the earliest date
            modDate = dateB.CompareTo(dateA) <= 0 ? dateB : dateA;
        }
        else
        {
            //assign the earliest date
            var oldestTxDate = dateB.CompareTo(dateA) <= 0 ? dateB : dateA;
            modDate = oldestTxDate.CompareTo(modDate) <= 0 ? oldestTxDate : modDate;
        }
        graph.ModifyFromDate = modDate;
    }

    public async Task ApplyModification()
    {
        graph.GraphStatus = GraphStatus.Modifying;
        var modDate = graph.ModifyFromDate;

        var dataCountBefore = graph.DataPointsPortfolio.Count;

        graph.DataPointsPortfolio.RemoveAll(x => x.Date >= modDate);
        graph.DataPointsInFlow.RemoveAll(x => x.Date >= modDate);
        graph.DataPointsOutFlow.RemoveAll(x => x.Date >= modDate);

        var dataCountAfter = graph.DataPointsPortfolio.Count;

        graph.ModifyFromDate = DateOnly.MinValue;
        graph.IsModificationRequested = false;
        graph.GraphStatus = GraphStatus.Idle;

    }

    public ObservableCollection<DateTimePoint> GetPortfolioValues()
    {
        var values = new ObservableCollection<DateTimePoint>();

        if (graph is null) { return values; }

        for (int i = 0; i < graph.DataPointsPortfolio.Count; i++)
        {
            var dateTimePoint = new DateTimePoint(graph.DataPointsPortfolio[i].Date.ToDateTime(TimeOnly.Parse("01:00 AM")), graph.DataPointsPortfolio[i].Value);
            values.Add(dateTimePoint);
        }
        return values;
    }
    
    public ObservableCollection<DateTimePoint> GetInFlowValues()
    {
        var values = new ObservableCollection<DateTimePoint>();
        if (graph is null) { return values; }

        for (int i = 0; i < graph.DataPointsInFlow.Count; i++)
        {
            var dateTimePoint = new DateTimePoint(graph.DataPointsInFlow[i].Date.ToDateTime(TimeOnly.Parse("01:00 AM")), graph.DataPointsInFlow[i].Value);
            values.Add(dateTimePoint);
        }
        return values;
    }
    
    public ObservableCollection<DateTimePoint> GetOutFlowValues()
    {
        var values = new ObservableCollection<DateTimePoint>();
        if (graph is null) { return values; }
        for (int i = 0; i < graph.DataPointsOutFlow.Count; i++)
        {
            var dateTimePoint = new DateTimePoint(graph.DataPointsOutFlow[i].Date.ToDateTime(TimeOnly.Parse("01:00 AM")), graph.DataPointsOutFlow[i].Value);
            values.Add(dateTimePoint);
        }
        return values;
    }

    public bool IsModificationRequested()
    {
        return  graph is not null ? graph.IsModificationRequested : false;
    }

    public DateOnly GetModifyFromDate()
    {
        return graph is not null ? graph.ModifyFromDate : DateOnly.MinValue;
    }

    public void AddDataPointInFlow(DataPoint dataPoint)
    {
        graph.DataPointsInFlow.Add(dataPoint);
    }
    public void AddDataPointOutFlow(DataPoint dataPoint)
    {
        graph.DataPointsOutFlow.Add(dataPoint);
    }
    public void AddDataPointPortfolio(DataPoint dataPoint)
    {
        graph.DataPointsPortfolio.Add(dataPoint);
    }

    public void AddHistoricalDataToBuffer(HistoricalDataByIdRev historicalData)
    {
        if (historicalData.DataPoints is not null )
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
        return graph.DataPointsPortfolio.OrderByDescending(x => x.Date).First().Date;
    }

    public DataPoint GetLatestDataPointInFlow()
    {
        return graph.DataPointsInFlow.OrderByDescending(x => x.Date).FirstOrDefault();
    }
    public DataPoint GetLatestDataPointOutFlow()
    {
        return graph.DataPointsOutFlow.OrderByDescending(x => x.Date).FirstOrDefault();
    }
    public bool HasDataPoints()
    {

        return graph is not null ? graph.DataPointsPortfolio.Count > 0 : false;
    }
    public bool HasHistoricalDataBuffer()
    {
        return graph is not null ? HistoricalDataByIdsBufferList.Count > 0 : false;
    }

    public int GetHistoricalDataBufferDatesCount()
    {
        return HistoricalDataByIdsBufferList.First().DataPoints.Count;
    }

    public DateOnly GetHistoricalDataBufferLatestDate()
    {
        var latestDataPoint = HistoricalDataByIdsBufferList.First().DataPoints.OrderByDescending(x => x.Date).FirstOrDefault();
    
        return latestDataPoint is not null ? latestDataPoint.Date : DateOnly.MinValue;

    }

    public DateOnly GetHistoricalDataBufferOldestDate()
    {
        var oldestDataPoint = HistoricalDataByIdsBufferList.First().DataPoints.OrderByDescending(x => x.Date).LastOrDefault();

        return oldestDataPoint is not null ? oldestDataPoint.Date : DateOnly.MinValue;

    }

    public Graph GetGraph()
    {
        return graph;

    }

    public async Task SetGraph(Graph graph)
    {
        this.graph = graph;
        await SaveGraphToJson();
        
    }

    public async Task<Graph> GetGraphFromJson()
    {
        Graph _graph = new();
        try
        {
            IsLoadingFromJson = true;

            if (!Directory.Exists(chartsFolder))
            {
                Directory.CreateDirectory(chartsFolder);
            }
            var fileName = chartsFolder + "\\graph.json";
            if (File.Exists(fileName))
            {
                using FileStream openStream = File.OpenRead(fileName);
                _graph = await JsonSerializer.DeserializeAsync<Graph>(openStream);
                Logger.Information("Graph data de-serialized succesfully ({0} data points)", _graph.DataPointsPortfolio.Count);
            }

            fileName = chartsFolder + "\\HistoryBuffer.json";
            if (File.Exists(fileName))
            {
                using FileStream openStream = File.OpenRead(fileName);
                HistoricalDataByIdsBufferList = await JsonSerializer.DeserializeAsync<List<HistoricalDataByIdRev>>(openStream);
            }

        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to de-serialize HGraph data");
        }
        finally
        {
            IsLoadingFromJson = false;
        }
        return _graph;
    }



}