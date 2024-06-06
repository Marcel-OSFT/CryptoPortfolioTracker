using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;

using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using WinUI3Localizer;
using System.Globalization;
using CryptoPortfolioTracker.Services;

namespace CryptoPortfolioTracker.ViewModels;

public partial class GraphicViewModel : BaseViewModel, IDisposable
{
    private readonly ILocalizer loc = Localizer.Get();

    public static GraphicViewModel Current;

    private readonly IGraphService _graphService;

    private ObservableCollection<DateTimePoint> valuesPortfolio = new();
    private ObservableCollection<DateTimePoint> valuesInFlow = new();
    private ObservableCollection<DateTimePoint> valuesOutFlow = new();

    [ObservableProperty] public ObservableCollection<ISeries> series = new();

    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }

    [ObservableProperty] public int progressValue;

    [ObservableProperty] public bool isFinishedLoading;

    partial void OnIsFinishedLoadingChanged(bool oldValue, bool newValue)
    {
        if (IsFinishedLoading)
        {
            GetValues();
            SetSeries();
        }
    }


    public GraphicViewModel(IGraphService graphService, IPreferencesService preferencesService) : base(preferencesService) 
    {
        Current = this;
        _graphService = graphService;
        IsFinishedLoading = _graphService.HasDataPoints();
        SetAxes();
        
    }

    public void Dispose()
    {
        Current = null;
    }

    public SolidColorPaint LegendTextPaint { get; set; } =
        new SolidColorPaint
        {
            Color = SKColors.DarkGoldenrod,
            SKTypeface = SKTypeface.FromFamilyName("Times New Roman")
        }; 
    

    public SolidColorPaint LedgendBackgroundPaint { get; set; } =
        new SolidColorPaint(SKColors.LightGoldenrodYellow);

    private void GetValues()
    {
        valuesPortfolio = new ObservableCollection<DateTimePoint>(_graphService.GetPortfolioValues());
        valuesInFlow = new ObservableCollection<DateTimePoint>(_graphService.GetInFlowValues());
        valuesOutFlow = new ObservableCollection<DateTimePoint>(_graphService.GetOutFlowValues());
    }


    private void SetSeries()
    {
        if (Series is not null) 
        { 
            Series.Clear();
            Series = null;
        }
        Series = new ObservableCollection<ISeries>
        {
            new LineSeries<DateTimePoint>
            {
                Values = valuesPortfolio,
                GeometrySize = 0,
                Stroke = new SolidColorPaint(SKColors.DarkGoldenrod) { StrokeThickness = 2 },
                Name = loc.GetLocalizedString("GraphicView_PortfolioSeriesTitle"), //"Portfolio Value",
                GeometryFill = new SolidColorPaint(),
                GeometryStroke = new SolidColorPaint(SKColors.DarkGoldenrod) { StrokeThickness = 4 }
            },
            new ColumnSeries<DateTimePoint>
            {
                Values = valuesInFlow,
                Stroke = new SolidColorPaint(SKColors.Yellow) { StrokeThickness = 4 },
                Fill = new SolidColorPaint(SKColors.Yellow),

                Name = loc.GetLocalizedString("GraphicView_InFlowSeriesTitle"), //"Inflow",
            },
            new ColumnSeries<DateTimePoint>
            {
                Values = valuesOutFlow,
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 },
                Fill = new SolidColorPaint(SKColors.Green),
                Name = loc.GetLocalizedString("GraphicView_OutFlowSeriesTitle"), //"Outflow",
            }
        };
        
    }

    private void SetAxes()
    {
        var ci = new CultureInfo(App.Localizer.GetCurrentLanguage());

        XAxes = new Axis[]
        {
            new DateTimeAxis(TimeSpan.FromDays(1), date => date.ToString(loc.GetLocalizedString("GraphicView_DateFormat"), ci))
            {
                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                    FontFamily = "Times New Roman",
                    SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                },
            }
        };
     
        YAxes = new Axis[]
        {
            new Axis
            {
                Name = loc.GetLocalizedString("GraphicView_PortfolioSeriesTitle"),
                NamePadding = new LiveChartsCore.Drawing.Padding(0, 15),

                NamePaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                },

                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                    FontFamily = "Times New Roman",
                    SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                },

                Labeler = value => string.Format("$ {0:N0}", value)
            }
            
        };
    
    }



}
