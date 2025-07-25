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
        SetupTeachingTips();
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


    private void SetupTeachingTips()
    {
        var teachingTipInitial = _viewModel._preferencesService.GetTeachingTip("TeachingTipBlank");
        var teachingTipRsi = _viewModel._preferencesService.GetTeachingTip("TeachingTipRsiHeat");
        var teachingTipEma = _viewModel._preferencesService.GetTeachingTip("TeachingTipEmaHeat");

        if (teachingTipInitial == null || !teachingTipInitial.IsShown)
        {
            _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipRsiHeat");

        }
        else if (teachingTipRsi != null && !teachingTipRsi.IsShown)
        {
            MyTeachingTipRsi.IsOpen = true;
        }
        else if (teachingTipEma != null && !teachingTipEma.IsShown)
        {
            MyTeachingTipEma.IsOpen = true;
        }
    }

    private void HeatMapType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        //_viewModel.ChangeHeatMapType(rbtHeatMap.SelectedIndex);
    }

    private void OnGetItClickedRsi(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipRsi.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipRsiHeat");

        // Navigate to the new feature or provide additional information
    }
    private void OnGetItClickedEma(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipEma.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipEmaHeat");

        // Navigate to the new feature or provide additional information
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    
}
