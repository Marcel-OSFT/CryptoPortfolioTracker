
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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    [ObservableObject]
    public partial class GraphService : IGraphService
    {
        private Graph graph;
        private static ILogger Logger { get; set; }
        private List<HistoricalDataById> HistoricalDataByIdsBufferList { get; set; }
        [ObservableProperty] bool isLoadingFromJson;

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

                if (!Directory.Exists(App.appDataPath + "\\MarketCharts"))
                {
                    Directory.CreateDirectory(App.appDataPath + "\\MarketCharts");
                }
                var fileName = App.appDataPath + "\\MarketCharts\\graph.json";
                if (File.Exists(fileName))
                {
                    using FileStream openStream = File.OpenRead(fileName);
                    graph = await JsonSerializer.DeserializeAsync<Graph>(openStream);
                    Logger.Information("Graph data de-serialized succesfully ({0} data points)", graph.DataPointsPortfolio.Count);
                }

                fileName = App.appDataPath + "\\MarketCharts\\HistoryBuffer.json";
                if (File.Exists(fileName))
                {
                    using FileStream openStream = File.OpenRead(fileName);
                    HistoricalDataByIdsBufferList = await JsonSerializer.DeserializeAsync<List<HistoricalDataById>>(openStream);
                }

                if (graph is null)
                {
                    graph = new();
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

        public async Task SaveGraphToJson()
        {
            var fileName = App.appDataPath + "\\MarketCharts\\graph.json";
            await using FileStream createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, graph);
        }

        public async Task SaveHistoricalDataBufferToJson()
        {
            var fileName = App.appDataPath + "\\MarketCharts\\HistoryBuffer.json";
            await using FileStream createStream = File.Create(fileName);
            await JsonSerializer.SerializeAsync(createStream, HistoricalDataByIdsBufferList);
        }

        public void ClearHistoricalDataBuffer()
        {
            HistoricalDataByIdsBufferList.Clear();

            if (System.IO.File.Exists(App.appDataPath + "\\MarketCharts\\HistoryBuffer.json"))
            {
                System.IO.File.Delete(App.appDataPath + "\\MarketCharts\\HistoryBuffer.json");
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

        public void AddHistoricalDataToBuffer(HistoricalDataById historicalData)
        {
            HistoricalDataByIdsBufferList.Add(historicalData);
        }

        public List<HistoricalDataById> GetHistoricalDataBuffer()
        {
            return HistoricalDataByIdsBufferList;
        }

        public DateOnly GetLatestDataPointDate()
        {
            return graph.DataPointsPortfolio.OrderByDescending(x => x.Date).First().Date;
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
            return HistoricalDataByIdsBufferList.First().Dates.Count;
        }

        public DateOnly GetHistoricalDataBufferLatestDate()
        {
            return HistoricalDataByIdsBufferList.First().Dates.LastOrDefault();
        }

    }
}
