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
    public static double[] SeparatorsTarget = new double[5];
    public static double[] SeparatorsRsi = new double[5];
    private ObservableCollection<HeatMapPoint> HeatMapPoints = new();

    private double bubbleSizeMax = 100;
    private double bubbleSizeMin = 1;

    [ObservableProperty] public int selectedHeatMapIndex = -1;

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
           // MinLimit = 0,
            Labeler = value => string.Empty,
        }
    };

    public RectangularSection[] sectionsTarget = new RectangularSection[]
    {
        new()
        {
            // creates a section from 3 to 4 in the y axis
            Yi = 0,
            Yj = 0,
            //Fill = new SolidColorPaint(new SKColor(180,207,186)),
            Fill = new SolidColorPaint(SKColors.DarkOliveGreen.WithAlpha(100)),

        },
    };

    public RectangularSection[] sectionsRsi = new RectangularSection[]
        {
        new()
        {
            // creates a section from 3 to 4 in the y axis
            Yi = 0,
            Yj = 0,
            //Fill = new SolidColorPaint(new SKColor(180,207,186)),
            Fill = new SolidColorPaint(SKColors.DarkOliveGreen.WithAlpha(100)),

        },
        };

    public Axis[] yAxesTarget = new Axis[]
    {
        new Axis
        {
            MinLimit=-100,
            MaxLimit=10,
            Name = loc.GetLocalizedString("HeatMapView_Target_YAxisTitle"),
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 0),

            NamePaint = new SolidColorPaint
            {
                Color = SKColors.DarkGoldenrod,
            },
            NameTextSize = 15,
            TextSize = 12,
            LabelsPaint = new SolidColorPaint
            {
                Color = SKColors.DarkGoldenrod,
                FontFamily = "Times New Roman",
                SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
            },
            Padding =  new LiveChartsCore.Drawing.Padding(1),
            Labeler = value => string.Format("{0:N0}", value),
            SeparatorsPaint = new SolidColorPaint
            {
                Color = SKColors.DarkGoldenrod,
                StrokeThickness = 1
            },
        }

    };

    public Axis[] yAxesRsi = new Axis[]
    {
        new Axis
        {
            MinLimit=0,
            MaxLimit=100,
            Name = loc.GetLocalizedString("HeatMapView_Rsi_YAxisTitle"),
            NamePadding = new LiveChartsCore.Drawing.Padding(0, 0),

            NamePaint = new SolidColorPaint
            {
                Color = SKColors.DarkGoldenrod,
            },
            NameTextSize = 15,
            TextSize = 12,
            LabelsPaint = new SolidColorPaint
            {
                Color = SKColors.DarkGoldenrod,
                FontFamily = "Times New Roman",
                SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
            },
            Padding =  new LiveChartsCore.Drawing.Padding(1),
            Labeler = value => string.Format("{0:N0}", value),
            SeparatorsPaint = new SolidColorPaint
            {
                Color = SKColors.DarkGoldenrod,
                StrokeThickness = 1
            },
        }

    };

    

    private void SetCustomSeparators()
    {
        //*** Target
        var withinRangePerc = _preferencesService.GetWithinRangePerc();
        SeparatorsTarget = new double[] { -100, -50, -1 * withinRangePerc, 0, 100 };

        var targetSection = sectionsTarget.First();
        targetSection.Yi = SeparatorsTarget[2];
        targetSection.Yj = SeparatorsTarget[3];

        var targetAxis = yAxesTarget.First();
        targetAxis.CustomSeparators = SeparatorsTarget;

        //*** RSI
        SeparatorsRsi = new double[] { 0, 30, 50, 70, 100 };

        var rsiSection = sectionsRsi.First();
        rsiSection.Yi = SeparatorsRsi[1];
        rsiSection.Yj = SeparatorsRsi[3];

        var rsiAxis = yAxesRsi.First();
        rsiAxis.CustomSeparators = SeparatorsRsi;
    }

    /// <summary>   
    /// This method is called by the HeatMapControl_Loading event.  
    /// </summary>
    public async void HeatMapControlLoading()
    {
        try
        {
            bubbleSizeMax = 50;
            bubbleSizeMin = 1;
            SetCustomSeparators();
            SelectedHeatMapIndex = _preferencesService.GetHeatMapIndex();
            await SetSeriesHeatMap(SelectedHeatMapIndex);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private async Task RefreshHeatMapPoints()
    {
        
        if (!SeriesHeatMap.Any())
        {
            await SetSeriesHeatMap(SelectedHeatMapIndex);
        }
        else
        {
            HeatMapPoints = new ObservableCollection<HeatMapPoint>(await _priceLevelService.GetHeatMapPoints(SelectedHeatMapIndex));
            SeriesHeatMap[0].Values = HeatMapPoints;
        }
    }

    private async Task SetSeriesHeatMap(int selectedHeatMapIndex)
    {
        var loc = Localizer.Get();

        SolidColorPaint labelColor = _preferencesService.GetAppTheme() ==  ElementTheme.Dark ? new SolidColorPaint(SKColors.White) : new SolidColorPaint(SKColors.Black);

        HeatMapPoints = new ObservableCollection<HeatMapPoint>(await _priceLevelService.GetHeatMapPoints(selectedHeatMapIndex));

        //*** Introduce dummyPoints for a second (not-visible) serie to force Bubble Geometry for weight 100
        //*** and MinGeometry to weight 1. In this way interpolation is in range 1 to 100.
        //*** example: 3 almost equal weights will now return in also 3 almost equal sized bubbels.
        HeatMapPoint dummyMax = new()
        {
            X = 0,
            Y = -150,
            Weight = 100
        };

        HeatMapPoint dummyMin = new()
        {
            X = 0,
            Y = -150,
            Weight = 1
        };

        var dummyPoints = new ObservableCollection<HeatMapPoint>() {dummyMin, dummyMax};

        if (HeatMapPoints is null || HeatMapPoints.Count == 0) return;
      
        if (selectedHeatMapIndex == 0)
        {
            var maxYValuePoint = HeatMapPoints.OrderByDescending(x => x.Y).FirstOrDefault();

            YAxesHeatMap.First().MaxLimit = maxYValuePoint?.Y > 10 ? maxYValuePoint.Y + 10 : 10;

            //*** Normalize the value to a multiple of 10
            YAxesHeatMap.First().MaxLimit = (double)Math.Floor((decimal)(YAxesHeatMap.First().MaxLimit ?? 0) / 10) * 10;
            var separators = YAxesHeatMap.First().CustomSeparators?.ToArray() ?? Array.Empty<double>();

            var maxLimit = YAxesHeatMap.First().MaxLimit ?? 0;
            separators[^1] = (double)maxLimit;
            YAxesHeatMap.First().CustomSeparators = separators;
        }

        HasTargets = HeatMapPoints.Count > 0;

        SeriesHeatMap?.Clear();

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
        if (selectedHeatMapIndex == 0)
        {
            serie.YToolTipLabelFormatter = (point) => $"{point.Model!.Label} {point.Model!.Y:N2} % ({point.Model!.Weight:N1} % of Portfolio)";
        }
        else
        {
            serie.YToolTipLabelFormatter = (point) => $"{point.Model!.Label}  RSI: {point.Model!.Y:N2}";
        }
        return ;

    }

    public void ChangeBubbleSize(SizeChangedEventArgs e)
    {
        if (YAxesHeatMap is null) return;

        var bubbleScale = Math.Min(Math.Max(e.NewSize.Width / 317, 1), 6);
        //var bubbleSizeMax = Math.Min(Math.Max(e.NewSize.Width / 12, 20), 200);
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
        _preferencesService.SetHeatMapIndex(index);
        SelectedHeatMapIndex = index;

        if (SelectedHeatMapIndex == 0)
        {
            HeatMapTitle = "Target Bubbles";
            SectionsHeatMap = sectionsTarget;
            YAxesHeatMap = yAxesTarget;
            await SetSeriesHeatMap(SelectedHeatMapIndex);
        }
        else
        {
            HeatMapTitle = "RSI Bubbles";
            SectionsHeatMap = sectionsRsi;
            YAxesHeatMap = yAxesRsi;
            await SetSeriesHeatMap(SelectedHeatMapIndex);
        }
    }




}
