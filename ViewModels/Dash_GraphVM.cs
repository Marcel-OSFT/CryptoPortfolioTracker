using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;

using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinUI;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using WinUI3Localizer;
using System.Globalization;
using TemperatureMonitor.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using TemperatureMonitor.Models;
using Microsoft.UI.Xaml;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.SKCharts;
using TemperatureMonitor.Helpers;
using System.Linq;

namespace TemperatureMonitor.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private ObservableCollection<DateTimePoint> chartValues = new();
    public ObservableCollection<DateTimePoint> ChartValues
    {
        get => chartValues;
        private set
        {
            chartValues = value;
            OnPropertyChanged(nameof(ChartValues));
        }
    }
    private static Func<double, string> labelerYAxis = value => string.Format("{0:N2} °C", value);

    [ObservableProperty] public ObservableCollection<ISeries> seriesGraph = new();
    public Axis[] XAxesGraph { get; set; } = Array.Empty<Axis>();
    public Axis[] YAxesGraph { get; set; } = Array.Empty<Axis>();

    public SolidColorPaint LegendTextPaintGraph { get; set; } = new()
    {
        Color = SKColors.DarkGoldenrod,
        SKTypeface = SKTypeface.FromFamilyName("Times New Roman")
    };

    [ObservableProperty] public double legendTextSizeGraph = 12;


    public void ConstructGraph()
    {
        SetYAxesGraph();
        SetXAxesGraph();
    }

    /// <summary>   
    /// This method is called by the GraphControl_Loaded event.  
    /// </summary>
    public async Task GraphControlLoaded()
    {
        //var loc = Localizer.Get();
        //var ci = new CultureInfo(loc.GetCurrentLanguage());
        try
        {
            await GetValuesGraph(DateOnly.FromDateTime(SelectedDate.Date));

            if (ChartValues.Any())
            {
                SetSeriesGraph();
                var ci = new CultureInfo(AppSettings.AppCultureLanguage);
                var last = _graphService.CurrentDayTemperatures.LastOrDefault();
                LastReadingTimestamp = last is null
                    ? "No readings available"
                    : last.Timestamp.ToLocalTime().ToString("G", ci);     // _graphService.GetLastReadingTimestamp();
                LastTemperatureReading = last is null
                    ? "No readings available"
                    : last.Value.ToString("N2", ci) + " °C";  //_graphService.GetLastTemperatureReading();
            }
        }
        catch (Exception)
        {
            
        }
    }

    private async Task UpdateSeriesValues()
    {
        //adjust SelectedDate in case a new day starts and "Today' is selected 
        if (isTodaySelected && DateTime.Now.Date != SelectedDate.Date)
        {
            SelectedDate = DateTime.Now.Date;
        }
        await GetValuesGraph(DateOnly.FromDateTime(SelectedDate.Date));

        SeriesGraph[0].Values = CurrentMode == DaySelectorMode.Nu ? ChartValues.Where(t => t.DateTime >= nowStart).ToList() : ChartValues;
        var ci = new CultureInfo(AppSettings.AppCultureLanguage);
        var last = _graphService.CurrentDayTemperatures.LastOrDefault();
        LastReadingTimestamp = last is null
            ? "No readings available"
            : last.Timestamp.ToLocalTime().ToString("G", ci);     // _graphService.GetLastReadingTimestamp();
        LastTemperatureReading = last is null
            ? "No readings available"
            : last.Value.ToString("N2", ci) + " °C";  //_graph_service.GetLastTemperatureReading();
    }

    private void SetSeriesGraph()
    {
        //var loc = Localizer.Get();

        //await GetValuesGraph();
        if (!ChartValues.Any())
        {
            SeriesGraph = new ObservableCollection<ISeries>();
            return;
        }
        SeriesGraph = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {   Tag = "Temperature",
                LineSmoothness=0.2,
                MiniatureShapeSize=2,
                Values = ChartValues,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.DarkGoldenrod) { StrokeThickness = 2 },
                Name = "",
            }
        };
        
    }

    private async Task GetValuesGraph(DateOnly date)
    {
        ChartValues = new ObservableCollection<DateTimePoint>(await _graphService.GetValues(date));
    }

    private void SetXAxesGraph()
    {
        var loc = Localizer.Get();
        var ci = new CultureInfo(AppSettings.AppCultureLanguage);   // App.Localizer.GetCurrentLanguage());

        if (XAxesGraph is not null)
        {
            XAxesGraph = Array.Empty<Axis>();
        }
        XAxesGraph = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromHours(1), date => date.ToString(loc.GetLocalizedString("GraphicView_DateFormat"), ci))
            {
                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                    FontFamily = "Times New Roman",
                    SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                },
                TextSize=12,
                Padding = new LiveChartsCore.Drawing.Padding(0,0),
            }
        };
    }

  
    private void SetYAxesGraph()
    {
        var loc = Localizer.Get();

        YAxesGraph = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                NamePadding = new LiveChartsCore.Drawing.Padding(0, 0),
                NameTextSize = 0,
                TextSize = 12,
                NamePaint = CreateSolidColorPaint(SKColors.White),
                LabelsPaint = CreateSolidColorPaint(SKColors.DarkGoldenrod),
                Labeler = labelerYAxis,
            }
        };
    }

    private SolidColorPaint CreateSolidColorPaint(SKColor color)
    {
        return new SolidColorPaint
        {
            Color = color,
            FontFamily = "Times New Roman",
            SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
        };
    }

    public void ChangeLabelSizeGraph(SizeChangedEventArgs e)
    {
        LegendTextSizeGraph = Math.Min(Math.Max(e.NewSize.Width / 25, 11), 20);

        var miniatureSize = Math.Round(Math.Min(Math.Max(e.NewSize.Width / 70, 4), 10));
        var labelSize = Math.Round(Math.Min(Math.Max(e.NewSize.Width / 25, 10), 18));

        foreach (var series in SeriesGraph)
        {
            series.MiniatureShapeSize = miniatureSize;
        }

        if (YAxesGraph?.FirstOrDefault() is Axis yAxis)
        {
            yAxis.TextSize = labelSize;
        }
        if (XAxesGraph?.FirstOrDefault() is Axis xAxis)
        {
            xAxis.TextSize = labelSize;
        }

    }


}
