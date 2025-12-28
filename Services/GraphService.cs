
using CommunityToolkit.Mvvm.ComponentModel;
using TemperatureMonitor.Enums;
using TemperatureMonitor.Models;
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

namespace TemperatureMonitor.Services;

public partial class GraphService : ObservableObject, IGraphService
{
    private Graph TemperatureGraph { get; set; } = new();
    public DateOnly selectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public List<DataPoint> CurrentDayTemperatures { get; private set; } = new();
    public List<DataPoint> ViewedDayTemperatures { get; private set; } = new();


    [ObservableProperty] private bool isLoadingFromJson;
    
    public GraphService() 
    {
       
    }

    public async Task<ObservableCollection<DateTimePoint>> GetValues(DateOnly date)
    {
        var values = new ObservableCollection<DateTimePoint>();

        if (TemperatureGraph == null) return values;
        return await LoadArchivedDayAsync(date);

    }

    /// <summary>
    /// Load archived readings for the specified calendar day and display them on the graph.
    /// - date: calendar day to load (local-date if DailyArchiveService was called with useLocalDate:true)
    /// </summary>
    public async Task<ObservableCollection<DateTimePoint>> LoadArchivedDayAsync(DateOnly date, CancellationToken ct = default)
    {
        // Load archived points for that day
        var points = await DailyArchiveService.LoadDayAsync(date, AppConstants.AppDataPath, useLocalDate: true, ct).ConfigureAwait(false);

        // Order by timestamp (UTC) for consistent presentation
        ViewedDayTemperatures = points.OrderBy(p => p.Timestamp.ToUniversalTime()).ToList();

        // If the requested day is today, update the current-day list
        if (date == DateOnly.FromDateTime(DateTime.Now))
        {
            CurrentDayTemperatures = ViewedDayTemperatures.ToList();
        }

        // Convert to DateTimePoint for the chart. Use local time so the X axis shows local time-of-day.
        var dtPoints = ViewedDayTemperatures
            .Select(p => new DateTimePoint(p.Timestamp.ToLocalTime(), p.Value))
            .ToList();

        // Update the chart collection (this will be bound to the series Values)
        return new ObservableCollection<DateTimePoint>(dtPoints);
    }

    public void AppendTemperaturesToCurrentDay(List<DataPoint> newTemperatures)
    {
        if (newTemperatures.Any())
        {
            CurrentDayTemperatures.AddRange(newTemperatures);
            CurrentDayTemperatures = CurrentDayTemperatures
                .OrderBy(p => p.Timestamp.ToUniversalTime())
                .ToList();
            
        }
    }


}