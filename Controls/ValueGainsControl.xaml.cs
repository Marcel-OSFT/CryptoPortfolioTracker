using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class ValueGainsControl : UserControl, INotifyPropertyChanged
{
    public readonly DashboardViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public ValueGainsControl()
    {
        InitializeComponent();
        _viewModel = DashboardViewModel.Current;
        DataContext = _viewModel;
    }

    private async void Control_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.GetValueGains();
        await _viewModel.GetCapitalFlowData();

    }

    private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.ChangeLabelSizeCapitalFlow(e);
    }

    


    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
