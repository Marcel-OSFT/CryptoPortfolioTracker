
using LiveChartsCore.Defaults;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TemperatureMonitor.Models;
using System;
using System.Collections.Generic;

namespace TemperatureMonitor.Services
{
    public interface IGraphService
    {
        bool IsLoadingFromJson { get; set; }
        List<DataPoint> CurrentDayTemperatures { get; }
        List<DataPoint> ViewedDayTemperatures { get; }

        void AppendTemperaturesToCurrentDay(List<DataPoint> newTemperatures);
        //string GetLastReadingTimestamp();
        //string GetLastTemperatureReading();

        // public Task LoadGraphFromJson();
        // public Task SaveGraphToJson();
        public Task<ObservableCollection<DateTimePoint>> GetValues(DateOnly date); 

        //public void AddDataPoint(DataPoint dataPoint);
        //bool HasDataPoints();
        Task<ObservableCollection<DateTimePoint>> LoadArchivedDayAsync(DateOnly date, CancellationToken ct = default);
    }
}
