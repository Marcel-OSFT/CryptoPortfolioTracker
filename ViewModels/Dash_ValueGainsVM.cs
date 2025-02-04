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
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using System.Linq;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Enums;
using System.Collections.Generic;
using System.Reflection.Emit;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Controls;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty] double totalValue;
    [ObservableProperty] double dayPerc;
    [ObservableProperty] double weekPerc;
    [ObservableProperty] double monthPerc;
    [ObservableProperty] double yearPerc;
    [ObservableProperty] double yearToDatePerc;
    [ObservableProperty] double costBase;
    [ObservableProperty] double costBasePerc;


    [ObservableProperty] double netCapitalFlow;
    [ObservableProperty] public ObservableCollection<ISeries> seriesCapFlow = new();
    [ObservableProperty] Axis[] xAxesCapitalFlow = Array.Empty<Axis>();
    [ObservableProperty] public double legendTextSizeCapitalFlow = 12;




    [ObservableProperty]
    Axis[] yAxesCapitalFlow = new Axis[]
        {
            new Axis
            {
                MinLimit = 0,
                NamePadding = new LiveChartsCore.Drawing.Padding(0, 0),
                NameTextSize = 0,
                TextSize = 12,
                NamePaint = new SolidColorPaint
                {
                    Color = SKColors.White,
                    FontFamily = "Times New Roman",
                    SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                },
                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                    FontFamily = "Times New Roman",
                    SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                },

                Labeler = labelerYAxis,
            }
        };

public SolidColorPaint LegendTextPaintCapitalFlow { get; set; } =
        new SolidColorPaint
        {
            Color = SKColors.DarkGoldenrod,
            SKTypeface = SKTypeface.FromFamilyName("Times New Roman")
        };

    /// <summary>   
    /// This method is called by the ValueGainsControl_Loaded event.  
    /// </summary>
    public async Task ValueGainsControlLoaded()
    {
        GetValueGains();
        await GetCapitalFlowData();
    }


    public void GetValueGains()
    {
        try
        {
            TotalValue = _dashboardService.GetPortfolioValue();

            CostBase = _dashboardService.GetCostBase();

            var dataPoints = _graphService.GetPortfolioValues();

            var currentDate = new DateTimePoint(DateTime.UtcNow,0);
            var previousDay = new DateTimePoint(currentDate.DateTime.AddDays(-1), 0);
            var previousWeek = new DateTimePoint(currentDate.DateTime.AddDays(-7), 0);
            var previousMonth = new DateTimePoint(currentDate.DateTime.AddDays(-30), 0);
            var previousYear = new DateTimePoint(currentDate.DateTime.AddYears(-1), 0);
            var yearToDate = new DateTimePoint(new DateTime(currentDate.DateTime.Year, 1, 1), 0);

            //var dayDataPoint = dataPoints.FirstOrDefault(dp => dp.DateTime.Date == previousDay.DateTime.Date);
            var dayDataPoint = dataPoints.LastOrDefault(); //take last available dataPoint
            var weekDataPoint = dataPoints.FirstOrDefault(dp => dp.DateTime.Date == previousWeek.DateTime.Date);
            var monthDataPoint = dataPoints.FirstOrDefault(dp => dp.DateTime.Date == previousMonth.DateTime.Date);
            var yearDataPoint = dataPoints.FirstOrDefault(dp => dp.DateTime.Date == previousYear.DateTime.Date);
            var yearToDateDataPoint = dataPoints.FirstOrDefault(dp => dp.DateTime.Date == yearToDate.DateTime.Date);

            DayPerc = dayDataPoint != null && dayDataPoint.Value.HasValue ? 100 * (TotalValue - dayDataPoint.Value.Value) / dayDataPoint.Value.Value : double.NegativeInfinity;
            WeekPerc = weekDataPoint != null && weekDataPoint.Value.HasValue ? 100 * (TotalValue - weekDataPoint.Value.Value) / weekDataPoint.Value.Value : double.NegativeInfinity;
            MonthPerc = monthDataPoint != null && monthDataPoint.Value.HasValue ? 100 * (TotalValue - monthDataPoint.Value.Value) / monthDataPoint.Value.Value : double.NegativeInfinity;
            YearPerc = yearDataPoint != null && yearDataPoint.Value.HasValue ? 100 * (TotalValue - yearDataPoint.Value.Value) / yearDataPoint.Value.Value : double.NegativeInfinity;
            YearToDatePerc = yearToDateDataPoint != null && yearToDateDataPoint.Value.HasValue ? 100 * (TotalValue - yearToDateDataPoint.Value.Value) / yearToDateDataPoint.Value.Value : double.NegativeInfinity;

            CostBasePerc = 100 * (TotalValue - CostBase) / CostBase;


        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while calculating value gains.");
        }
    }

    public async Task GetCapitalFlowData()
    {

        var deposits = await _dashboardService.GetYearlyMutationsByTransactionKind(TransactionKind.Deposit);
        var withdrawals = await _dashboardService.GetYearlyMutationsByTransactionKind(TransactionKind.Withdraw);

        NetCapitalFlow = deposits.Sum(x => x.Value) - withdrawals.Sum(x => x.Value);

        if (XAxesCapitalFlow is not null)
        {
            XAxesCapitalFlow = Array.Empty<Axis>();
        }
        XAxesCapitalFlow = new Axis[]
        {
            new Axis
            {
                Labels = new List<string>(deposits.Select(x => x.Year).ToList()),
                LabelsPaint = new SolidColorPaint
                {
                    Color = SKColors.DarkGoldenrod,
                    FontFamily = "Times New Roman",
                    SKFontStyle = new SKFontStyle(SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic)
                },
                TextSize = 12,
                Padding = new LiveChartsCore.Drawing.Padding(0, 0),
                LabelsRotation = 0,
            }
        };


        if (SeriesCapFlow is not null)
        {
            SeriesCapFlow.Clear();
        }
        SeriesCapFlow = new ObservableCollection<ISeries>
        {
            new ColumnSeries<double>
            {
                
                MiniatureShapeSize=2,
                Values = new ObservableCollection<double>(deposits.Select(x => x.Value).ToList()),
                Stroke = new SolidColorPaint(SKColors.Yellow) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Yellow.WithAlpha(220)),
                Name = loc.GetLocalizedString("GraphicView_InFlowSeriesTitle"), //"Inflow",
            },
            new ColumnSeries<double>
            {

                MiniatureShapeSize=2,
                Values = new ObservableCollection<double>(withdrawals.Select(x => x.Value).ToList()),
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 2 },
                Fill = new SolidColorPaint(SKColors.Green.WithAlpha(220)),
                Name = loc.GetLocalizedString("GraphicView_OutFlowSeriesTitle"), //"Inflow",
            },

        };

    }

    public void ChangeLabelSizeCapitalFlow(SizeChangedEventArgs e)
    {
        LegendTextSizeCapitalFlow = Math.Min(Math.Max(e.NewSize.Width / 25, 11), 20);

        var miniatureSize = Math.Round(Math.Min(Math.Max(e.NewSize.Width / 70, 4), 10));
        var labelSize = Math.Round(Math.Min(Math.Max(e.NewSize.Width / 25, 11), 18));

        foreach (var series in SeriesCapFlow)
        {
            series.MiniatureShapeSize = miniatureSize;
        }

        if (YAxesCapitalFlow?.FirstOrDefault() is Axis yAxis)
        {
            yAxis.TextSize = labelSize;
        }
        if (XAxesCapitalFlow?.FirstOrDefault() is Axis xAxis)
        {
            xAxis.TextSize = labelSize;
        }
       

    }


    private void ReloadValueGains()
    {
        OnPropertyChanged("TotalValue");
        OnPropertyChanged("CostBase");
        OnPropertyChanged("NetCapitalFlow");

        if (YAxesCapitalFlow is null) return;
        foreach (var axis in YAxesCapitalFlow)
        {
            axis.Labeler = labelerYAxis;
        }
        OnPropertyChanged("YAxisCapitalFlow");
        
    }
    
    

}
