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
using CryptoPortfolioTracker.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Xaml;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.SKCharts;
using CryptoPortfolioTracker.Helpers;
using System.Linq;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    //private readonly ILocalizer loc = Localizer.Get();

    
    private ObservableCollection<DateTimePoint> valuesPortfolio = new();
    private ObservableCollection<DateTimePoint> valuesInFlow = new();
    private ObservableCollection<DateTimePoint> valuesOutFlow = new();
    
    [ObservableProperty] public ObservableCollection<ISeries> seriesGraph = new();
    public Axis[] XAxesGraph { get; set; } = Array.Empty<Axis>();
    public Axis[] YAxesGraph { get; set; } = Array.Empty<Axis>();

    [ObservableProperty] public int progressValueGraph;
    [ObservableProperty] public bool isUpdatingGraph;
    [ObservableProperty] public bool isLoadingFromJsonGraph;
  

    public SolidColorPaint LegendTextPaintGraph { get; set; } = new()
    {
        Color = SKColors.DarkGoldenrod,
        SKTypeface = SKTypeface.FromFamilyName("Times New Roman")
    };

    [ObservableProperty] public double legendTextSizeGraph = 12;

    private static Func<double, string> labelerGraph = value => string.Format("$ {0:N0}", value);

    partial void OnIsUpdatingGraphChanged(bool value)
    {
        if (!IsUpdatingGraph) // && !_isInitializing)
        {
            SetValuesGraph();
        }
    }

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
        var loc = Localizer.Get();
        var ci = new CultureInfo(loc.GetCurrentLanguage());
        try
        {
            await WaitForJsonToLoad();
            
            if (_graphService.HasDataPoints())
            {
                SetSeriesGraph();
            }
            // re-set the labels because language settings might have changed
            XAxesGraph[0].Labeler = value => value.AsDate().ToString(loc.GetLocalizedString("GraphicView_DateFormat"), ci);
            YAxesGraph[0].Name = loc.GetLocalizedString("GraphicView_PortfolioSeriesTitle");
        }
        catch (Exception)
        {
            
        }
    }

    private async Task WaitForJsonToLoad()
    {
        while (_graphService.IsLoadingFromJson)
        {
            IsLoadingFromJsonGraph = true;
            await Task.Delay(100);
        }
        IsLoadingFromJsonGraph = false;
    }


    private void UpdateSeriesValues()
    {
        GetValuesGraph();

        SeriesGraph[0].Values = valuesPortfolio;
        SeriesGraph[1].Values = valuesInFlow;
        SeriesGraph[2].Values = valuesOutFlow;
    }

    private void SetSeriesGraph()
    {
        var loc = Localizer.Get();

        GetValuesGraph();

        if (SeriesGraph is not null) 
        { 
            SeriesGraph.Clear();
        }
        SeriesGraph = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {   Tag = "Portfolio",
                LineSmoothness=0.2,
                MiniatureShapeSize=2,
                Values = valuesPortfolio,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.DarkGoldenrod) { StrokeThickness = 2 },
                Name = loc.GetLocalizedString("GraphicView_PortfolioSeriesTitle"), //"Portfolio Value",
            },
            new ColumnSeries<DateTimePoint>
            {
                Tag = "Inflow",
                MiniatureShapeSize=2,
                Values = valuesInFlow,
                Stroke = new SolidColorPaint(SKColors.Yellow) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Yellow),
                Name = loc.GetLocalizedString("GraphicView_InFlowSeriesTitle"), //"Inflow",
            },
            new ColumnSeries<DateTimePoint>
            {
                Tag = "Outflow",
                MiniatureShapeSize=2,
                Values = valuesOutFlow,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Green),
                Name = loc.GetLocalizedString("GraphicView_OutFlowSeriesTitle"), //"Outflow",
            }
        };
        
    }

    private void GetValuesGraph()
    {
        valuesPortfolio = new ObservableCollection<DateTimePoint>(_graphService.GetPortfolioValues());
        valuesInFlow = new ObservableCollection<DateTimePoint>(_graphService.GetInFlowValues());
        valuesOutFlow = new ObservableCollection<DateTimePoint>(_graphService.GetOutFlowValues());
    }

    private void SetValuesGraph()
    {
        if (!SeriesGraph.Any())
        {
            SetSeriesGraph();
        }
        else
        {
            UpdateSeriesValues();
        }
    }

    private void SetXAxesGraph()
    {
        var loc = Localizer.Get();
        var ci = new CultureInfo(_preferencesService.GetAppCultureLanguage());   // App.Localizer.GetCurrentLanguage());

        if (XAxesGraph is not null)
        {
            XAxesGraph = Array.Empty<Axis>();
        }
        XAxesGraph = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString(loc.GetLocalizedString("GraphicView_DateFormat"), ci))
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

    private void ReloadGraph()
    {
        if (YAxesGraph is null) return;

        foreach (var axis in YAxesGraph)
        {
            axis.Labeler = labelerYAxis;
        }
        OnPropertyChanged("YAxisGraph");

    }

}
