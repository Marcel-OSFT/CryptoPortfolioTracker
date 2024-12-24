using CryptoPortfolioTracker.ViewModels;
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
        _viewModel = DashboardViewModel.Current;
        DataContext = _viewModel;
        InitializeComponent();
        
    }

    // ... other code ...

    private async void Control_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await _viewModel.InitializeGraph();
        
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


    





}
