using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WinUI3Localizer;
using System.Diagnostics;
using CryptoPortfolioTracker.Models;
using System.Linq;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.ViewModels;



public partial class DashboardViewModel : BaseViewModel
{
    private ObservableCollection<HeatMapPoint> HeatMapPoints = new();

    private double bubbleSizeMax = 100;
    private double bubbleSizeMin = 1;
    private const double BreakEvenMinimum = -1050;

    [ObservableProperty] public int selectedHeatMapIndex = -1;
    [ObservableProperty] public string maRbContent = string.Empty;
    [ObservableProperty] public string rsiRbContent = string.Empty;
    [ObservableProperty] public string breakEvenRbContent = string.Empty;
    
    async partial void OnSelectedHeatMapIndexChanged(int oldValue, int newValue)
    {
        if (newValue == -1) return;
        await ChangeHeatMapType(newValue);
    }

    [ObservableProperty] string heatMapTitle = string.Empty;

    [ObservableProperty] bool hasTargets = false;
    [ObservableProperty] public RectangularSection[] sectionsHeatMap;

    [ObservableProperty] public ObservableCollection<ISeries> seriesHeatMap = new();
    [ObservableProperty] public Axis[] yAxesHeatMap;

    [ObservableProperty]
    public Axis[] xAxesHeatMap = new Axis[]
    {
        new Axis
        {
            Labeler = value => string.Empty,
        }
    };

    public BubbleMap TargetBubbleMap { get; } = new(
        0,
        BubbleMapType.Target,
        "Target Bubbles",
        "HeatMapView_Target_YAxisTitle",
        -100,
        10,
        SKColors.DarkOliveGreen);

    public BubbleMap BreakEvenBubbleMap { get; } = new(
        3,
        BubbleMapType.BreakEven,
        "Break-Even Bubbles",
        "HeatMapView_BreakEven_YAxisTitle",
        -1100,
        100,
        SKColors.DarkOliveGreen);

    public BubbleMap RsiBubbleMap { get; } = new(
        1,
        BubbleMapType.Rsi,
        "RSI Bubbles",
        "HeatMapView_Rsi_YAxisTitle",
        0,
        100,
        SKColors.DarkOliveGreen);

    public BubbleMap MaBubbleMap { get; } = new(
        2,
        BubbleMapType.Ma,
        "MA Bubbles",
        "HeatMapView_Ema_YAxisTitle",
        -50,
        10,
        SKColors.DarkRed);

    private BubbleMap ActiveBubbleMap =>
        SelectedHeatMapIndex switch
        {
            0 => TargetBubbleMap,
            1 => RsiBubbleMap,
            2 => MaBubbleMap,
            3 => BreakEvenBubbleMap,
            _ => TargetBubbleMap
        };


    private void SetCustomSeparatorsAll()
    {
        var withinRangePerc = AppSettings.WithinRangePerc;


        TargetBubbleMap.Configure(withinRangePerc);
        BreakEvenBubbleMap.Configure(withinRangePerc);
        RsiBubbleMap.Configure(withinRangePerc);
        MaBubbleMap.Configure(withinRangePerc);
    }

    //private void SetCustomSeparatorsTarget()
    //{
    //    TargetBubbleMap.Configure(AppSettings.WithinRangePerc);
    //}

    //private void SetCustomSeparatorsBreakEven()
    //{
    //    BreakEvenBubbleMap.Configure(AppSettings.WithinRangePerc);
    //}

    //private void SetCustomSeparatorsRsi()
    //{
    //    RsiBubbleMap.Configure(AppSettings.WithinRangePerc);
    //}

    //private void SetCustomSeparatorsMa()
    //{
    //    MaBubbleMap.Configure(AppSettings.WithinRangePerc);
    //}


    /// <summary>   
    /// This method is called by the HeatMapControl_Loaded event.  
    /// </summary>
    public async void HeatMapControlLoaded()
    {
        try
        {
            bubbleSizeMax = 50;
            bubbleSizeMin = 1;
            SetCustomSeparatorsAll();
            RsiRbContent = $"1D RSI({AppSettings.RsiPeriod})";
            MaRbContent = $"1D {AppSettings.MaType}({AppSettings.MaPeriod})";
            BreakEvenRbContent = $"Break-even";
            SelectedHeatMapIndex = AppSettings.HeatMapIndex;
            await _dashboardService.EvaluatePriceLevels();
            await SetSeriesHeatMap(SelectedHeatMapIndex);
        }
        catch (Exception )
        {
        }
    }

    private async Task RefreshHeatMapPoints()
    {
        BubbleMap bubbleMap = ActiveBubbleMap;

        SetMinMaxY(bubbleMap, out ObservableCollection<HeatMapPoint> dummyPoints);

        HeatMapPoints = new ObservableCollection<HeatMapPoint>(await _priceLevelService.GetHeatMapPoints(bubbleMap));
        SeriesHeatMap[0].Values = HeatMapPoints;
        SeriesHeatMap[1].Values = dummyPoints;

    }

    private async Task SetSeriesHeatMap(int selectedHeatMapIndex)
    {
        var loc = Localizer.Get();
        BubbleMap bubbleMap = ActiveBubbleMap;

        SolidColorPaint labelColor = AppSettings.AppTheme == ElementTheme.Dark ? new SolidColorPaint(SKColors.White) : new SolidColorPaint(SKColors.Black);

        HeatMapPoints = new ObservableCollection<HeatMapPoint>(await _priceLevelService.GetHeatMapPoints(bubbleMap));

        SetMinMaxY(bubbleMap, out ObservableCollection<HeatMapPoint> dummyPoints);
        HasTargets = HeatMapPoints.Count > 0;

        SeriesHeatMap = new ObservableCollection<ISeries>
        {
           new ScatterSeries<HeatMapPoint>
            {
                Values = HeatMapPoints,
                StackGroup = 1,
                DataLabelsSize = 10,
                DataLabelsPaint = labelColor,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                DataLabelsPadding = new LiveChartsCore.Drawing.Padding(2),
                DataLabelsFormatter = (point) => $"{point.Model!.Label}",
                GeometrySize = bubbleSizeMax,
                MinGeometrySize = bubbleSizeMin,
                Stroke = new SolidColorPaint(SKColors.Goldenrod) { StrokeThickness = 2 },
                Fill = null,
            },
           new ScatterSeries<HeatMapPoint>
            {
                Values = dummyPoints,
                StackGroup = 1,
                GeometrySize = bubbleSizeMax,
                MinGeometrySize = bubbleSizeMin,
                Stroke = null,
                Fill = null,
            },

        };

        var serie = SeriesHeatMap.First() as ScatterSeries<HeatMapPoint>;

        serie.YToolTipLabelFormatter = point => bubbleMap.FormatTooltip(point.Model!);

        return;

    }

    private void SetMinMaxY(BubbleMap bubbleMap, out ObservableCollection<HeatMapPoint> dummyPoints)
    {
        // Introduce dummy points for a second invisible series.
        // This forces bubble geometry interpolation between weight 1 and 100.
        double dummyY = bubbleMap.Type == BubbleMapType.BreakEven
            ? BreakEvenMinimum
            : -150;

        HeatMapPoint dummyMax = new()
        {
            X = 0,
            Y = dummyY,
            Weight = 100
        };

        HeatMapPoint dummyMin = new()
        {
            X = 0,
            Y = dummyY,
            Weight = 1
        };

        dummyPoints =
        [
            dummyMin,
        dummyMax
        ];

        if (HeatMapPoints is null || HeatMapPoints.Count == 0)
        {
            return;
        }

        Axis axis = bubbleMap.YAxes.First();

        if (bubbleMap.Type == BubbleMapType.Target)
        {
            HeatMapPoint? maxYValuePoint = HeatMapPoints
                .OrderByDescending(point => point.Y)
                .FirstOrDefault();

            double maxLimit = (double)(maxYValuePoint is not null && maxYValuePoint.Y > 10
                ? maxYValuePoint.Y + 10
                : 10);

            axis.MaxLimit = (Math.Floor(maxLimit / 10) + 1) * 10;

            double[] separators = bubbleMap.Separators.ToArray();
            separators[^1] = axis.MaxLimit.Value;
            axis.CustomSeparators = separators;
        }
        else if (bubbleMap.Type == BubbleMapType.Ma)
        {
            HeatMapPoint? maxYValuePoint = HeatMapPoints
                .OrderByDescending(point => point.Y)
                .FirstOrDefault();

            double maxLimit = (double)(maxYValuePoint is not null && maxYValuePoint.Y > 10
                ? maxYValuePoint.Y + 10
                : 10);

            axis.MaxLimit = (Math.Floor(maxLimit / 10) + 1) * 10;

            double[] separators = bubbleMap.Separators.ToArray();
            separators[^1] = axis.MaxLimit.Value;

            HeatMapPoint? minYValuePoint = HeatMapPoints
                .OrderBy(point => point.Y)
                .FirstOrDefault();

            double minLimit = (double)(minYValuePoint is not null && minYValuePoint.Y < -10
                ? minYValuePoint.Y - 10
                : -10);

            axis.MinLimit = Math.Floor(minLimit / 10) * 10;
            separators[0] = axis.MinLimit.Value;

            axis.CustomSeparators = separators;
        }
    }

    public void ChangeBubbleSize(SizeChangedEventArgs e)
    {
        if (YAxesHeatMap is null) return;

        var bubbleScale = Math.Min(Math.Max(e.NewSize.Width / 317, 1), 6);
        var labelSize = Math.Min(Math.Max(e.NewSize.Width / 70, 10), 18);

        YAxesHeatMap.First().TextSize = labelSize;
        YAxesHeatMap.First().NameTextSize = labelSize;

        if (SeriesHeatMap != null)
        {
            foreach (var series in SeriesHeatMap)
            {
                if (series is ScatterSeries<HeatMapPoint> Series)
                {
                    Series.DataLabelsSize = labelSize;
                    Series.GeometrySize = bubbleSizeMax*bubbleScale;
                    Series.MinGeometrySize = bubbleSizeMin*bubbleScale;
                }
            }
        }
    }

    public async Task ChangeHeatMapType(int index)
    {
        AppSettings.HeatMapIndex = index;
        SelectedHeatMapIndex = index;

        var bubbleMap = ActiveBubbleMap;

        HeatMapTitle = bubbleMap.Title;
        SectionsHeatMap = bubbleMap.Sections;
        YAxesHeatMap = bubbleMap.YAxes;

        await SetSeriesHeatMap(index);
    }



}
