
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using System;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using System.Collections.Generic;

namespace CryptoPortfolioTracker.Services
{
    public interface IGraphService
    {
        bool IsLoadingFromJson { get; set; }
        public Task LoadGraphFromJson();
        public Task SaveGraphToJson();
        public Task SaveHistoricalDataBufferToJson();
        public Task RegisterModification(Transaction transactionA, Transaction? transactionB = null);
        public Task ApplyModification();
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
    }
}
