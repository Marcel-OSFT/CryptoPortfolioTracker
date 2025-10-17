using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace CryptoPortfolioTracker.Controls;

public partial class HeatMapControl : UserControl, INotifyPropertyChanged
{
    public readonly DashboardViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public HeatMapControl()
    {
        InitializeComponent();
        _viewModel = DashboardViewModel.Current ?? throw new InvalidOperationException("DashboardViewModel.Current is null");
        DataContext = _viewModel;
    }

    private void Control_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        //_viewModel.HeatMapControlLoading();
    }
    private void Control_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.HeatMapControlLoaded();
    }

    private void HeatMap_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.ChangeBubbleSize(e);
    }
   
    private void HeatMapType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //_viewModel.ChangeHeatMapType(rbtHeatMap.SelectedIndex);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    
}
