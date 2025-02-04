using CommunityToolkit.Common;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using WinUIEx;

namespace CryptoPortfolioTracker.Controls;

public partial class PieChartControl : UserControl, INotifyPropertyChanged
{
    public readonly DashboardViewModel _viewModel;
    public PieChart pieChart;

    public bool isFillSet = false;

    private ObservableCollection<ISeries> seriesPie = new ObservableCollection<ISeries>();
    public ObservableCollection<ISeries> SeriesPie
    {
        get { return seriesPie; }
        set
        {
            if (value != seriesPie)
            {
                seriesPie = value;
                OnPropertyChanged(nameof(SeriesPie));
            }
        }
    }

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public PieChartControl()
    {
        InitializeComponent();
        _viewModel = DashboardViewModel.Current;
        DataContext = _viewModel;
        pieChart = this.pie;
        _viewModel.PieChartControls.Add(this);
        
    }

    private void Control_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
       //_viewModel.PieControlLoading(this);
    }

    private void Control_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.PieControlLoaded(this);
    }

    private void Pie_UpdateStarted(IChartView<SkiaSharpDrawingContext> chart)
    {
        if (!isFillSet) SetFill();
    }


    private void Pie_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        //use the smallest size of the width and height of the pie chart
        var newSize = Math.Round(Math.Min(e.NewSize.Width, e.NewSize.Height));

        if (newSize == 0) return;

        var labelSize = Math.Round(Math.Min(Math.Max(newSize / 25, 10), 18));
        //var maxLabelWidth = Math.Round(Math.Min(Math.Max(newSize / 3, 100), 170));
        var truncate = Math.Round(Math.Min(Math.Max(newSize / 20, 9), 30));

        if (sender is PieChart pieChart && pieChart is not null)
        {
            if (SeriesPie == null) return;
            foreach (var series in SeriesPie)
            {
                if (series is PieSeries<PiePoint> _series && _series.Fill is SolidColorPaint paint)
                {
                    _series.DataLabelsSize = labelSize;
                    //_series.DataLabelsMaxWidth = maxLabelWidth; does not update!!?
                    _series.DataLabelsFormatter = point => $"{_series.Name.Truncate((int)truncate)}";

                }
            }
            
        }

    }

    private void SetFill()
    {
        // Get the default color and set the alpha channel for opacity
        //var color = defaultColors[i++ % defaultColors.Length];

        if (SeriesPie == null) return;
        {
            foreach(var series in SeriesPie)
            {
                if (series is PieSeries<PiePoint> _series && _series.Fill is SolidColorPaint paint)
                {   
                    //get assigned color and set opacity value 
                    var color = paint.Color;
                    var fillColor = new SKColor(color.Red, color.Green, color.Blue, 220); // 128 is 50% opacity

                    _series.Fill = new SolidColorPaint(fillColor);

                    isFillSet = true;
                }
            }

        }
    }



    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    
}
