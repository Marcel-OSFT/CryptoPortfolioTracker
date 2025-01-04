﻿using System.Collections.ObjectModel;
using System;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Diagnostics;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Controls;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.Measure;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Common;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    public List<PieChartControl> PieChartControls { get; set; } = new();

    private void ConstructPie()
    {
        
    }

    public void InitializePie(PieChartControl pieChart)
    {
        try
        {
            var language = _preferencesService.GetAppCultureLanguage();
            var headerTag = pieChart.pieHeader.Tag;

            pieChart.pieHeader.Text = loc.GetLocalizedString("PieChartView_" + headerTag);
            
            SetSeriesPie(pieChart);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private async void SetSeriesPie(PieChartControl pieChart)
    {
        var points = new ObservableCollection<PiePoint>(await _dashboardService.GetPiePoints(pieChart.Name));
        double totalValue = (double)points.Sum(p => p.Value); // Assuming PiePoint has a property 'Value'

        double sumSlice = 0;

        var random = new Random();
        

        //SeriesPiePortfolio = new ObservableCollection<ISeries>(PiePoints.AsPieSeries((value, series) =>
        pieChart.SeriesPie = new ObservableCollection<ISeries>(points.AsPieSeries((value, series) =>
            {
                series.Name = value.Label;
                series.DataLabelsPosition = PolarLabelsPosition.Middle;
                series.DataLabelsRotation = GetLabelAngle((double)value.Value, totalValue, ref sumSlice);
                series.DataLabelsSize = 12; //initial labelsize
                series.DataLabelsPaint = new SolidColorPaint(SKColors.Black);
                series.DataLabelsFormatter = point => $"{value.Label.Truncate(9)}"; //initial truncate
                series.DataLabelsMaxWidth = 300;
                series.ToolTipLabelFormatter = point => $"{point.StackedValue!.Share:P2}";
            })
        );

        pieChart.isFillSet = false;

    }

    private double GetLabelAngle(double sliceValue, double totalValue, ref double sumSlice)
    {
        var thisSliceAngle = (sliceValue / totalValue) * 360.0;

        //// negative angles do not work properly, so force them to be positive
        var angle =  (360.0 - sumSlice) - (0.5*thisSliceAngle) - 90.0;

        //// for readability prevent labels to be rotated more then 90 degrees, flip 180 degrees if they do
        if (angle < 0)
        {
            angle += 360;
        }
        else if (angle > 90) angle += 180;
        sumSlice += thisSliceAngle;
        return angle;
    }

}








