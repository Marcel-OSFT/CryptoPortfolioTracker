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
using System.Diagnostics;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.ViewModels;

public partial class GraphicViewModel : BaseViewModel
{
    //private readonly ILocalizer loc = Localizer.Get();

    public static GraphicViewModel Current;
    private readonly IGraphService _graphService;
    private ObservableCollection<DateTimePoint> valuesPortfolio = new();
    private ObservableCollection<DateTimePoint> valuesInFlow = new();
    private ObservableCollection<DateTimePoint> valuesOutFlow = new();
    [ObservableProperty] public ObservableCollection<ISeries> series = new();
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
    [ObservableProperty] public int progressValue;
    [ObservableProperty] public bool isUpdating;
    [ObservableProperty] public bool isLoadingFromJson;
    [ObservableProperty] public bool isNotUpdating;
    [ObservableProperty] public bool isNotLoadingFromJson;
    
    partial void OnIsLoadingFromJsonChanged(bool value)
    {
        IsNotLoadingFromJson = !value;
    }

    partial void OnIsUpdatingChanged(bool value)
    {
        if (!IsUpdating) // && !_isInitializing)
        {
            GetValues();
            SetSeries();
        }
        IsNotUpdating = !value;
    }

    public GraphicViewModel(IGraphService graphService, IPreferencesService preferencesService) : base(preferencesService) 
    {
        Current = this;
        _graphService = graphService;
        IsNotUpdating = !isUpdating;
        IsNotLoadingFromJson = !IsLoadingFromJson;
        SetYAxes();
        SetXAxes();
    }

    public async Task InitializeGraph()
    {
        var loc = Localizer.Get();
        var ci = new CultureInfo(loc.GetCurrentLanguage());
        try
        {
            await WaitForJsonToLoad();
            if (_graphService.HasDataPoints())
            {
                GetValues();
                SetSeries();
            }
            // re-set the labels because language settings might have changed
            XAxes[0].Labeler = value => value.AsDate().ToString(loc.GetLocalizedString("GraphicView_DateFormat"), ci);
            YAxes[0].Name = loc.GetLocalizedString("GraphicView_PortfolioSeriesTitle");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async Task WaitForJsonToLoad()
    {
        while (_graphService.IsLoadingFromJson)
        {
            IsLoadingFromJson = true;
            await Task.Delay(1000);
        }
        IsLoadingFromJson = false;
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
        var loc = Localizer.Get();

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

    private void SetXAxes()
    {
        var loc = Localizer.Get();
        var ci = new CultureInfo(App.Localizer.GetCurrentLanguage());

        if (XAxes is not null)
        {
            XAxes = null;
        }
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
    }

    private void SetYAxes()
    {
        var loc = Localizer.Get();

        if (YAxes is not null)
        {
            YAxes = null;
        }
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
