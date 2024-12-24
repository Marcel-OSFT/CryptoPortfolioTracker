using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CryptoPortfolioTracker.Models;


public class HeatMapPoint : WeightedPoint
{
    public HeatMapPoint()
    {
       Label=string.Empty;
    }

    //public ObservablePoint Point { get; set;}

    public string Label  { get; set; }
}


public class PiePoint : ObservableValue
{
    public PiePoint()
    {
        Label = string.Empty;
    }

    //public ObservablePoint Point { get; set;}

    public string Label { get; set; }
}