using CryptoPortfolioTracker.ViewModels;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class GraphControl : UserControl, INotifyPropertyChanged
{
    public readonly DashboardViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public GraphControl()
    {
        _viewModel = DashboardViewModel.Current ?? throw new InvalidOperationException("DashboardViewModel.Current is null");
        DataContext = _viewModel;
        InitializeComponent();
        _viewModel.ConstructGraph();
    }

    private async void Control_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
       // await _viewModel.GraphControlLoading();
    }

    private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.ChangeLabelSizeGraph(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.GraphControlLoaded();
    }
}
