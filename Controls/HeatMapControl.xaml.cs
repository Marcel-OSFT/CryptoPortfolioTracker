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

    private async void Control_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.InitializeHeatMap();
    }

    private void HeatMap_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.ChangeBubbleSize(e);
    }


    private void SetupTeachingTips()
    {
        var teachingTipInitial = _viewModel._preferencesService.GetTeachingTip("TeachingTipBlank");
        var teachingTipRsi = _viewModel._preferencesService.GetTeachingTip("TeachingTipRsiHeat");

        if (teachingTipInitial == null || !teachingTipInitial.IsShown)
        {
            _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipRsiHeat");

        }
        else if (teachingTipRsi != null && !teachingTipRsi.IsShown)
        {

            MyTeachingTipRsi.IsOpen = true;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
