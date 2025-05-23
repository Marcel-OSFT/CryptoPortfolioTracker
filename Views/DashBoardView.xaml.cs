using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;

namespace CryptoPortfolioTracker.Views;
[ObservableObject]
public partial class DashboardView : Page, IDisposable
{
   private readonly DashboardViewModel _viewModel;
    public static DashboardView? Current;
   

    public DashboardView(DashboardViewModel dashboardVm)
    {
        Current = this;
        InitializeComponent();
        _viewModel = dashboardVm;
        DataContext = _viewModel;
        Portfolio.pieHeader.Tag = "Portfolio";
        Accounts.pieHeader.Tag = "Accounts";
        Narratives.pieHeader.Tag = "Narratives";
        SetupTeachingTips();
    }

    private async void View_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.ViewLoading();
    }

    private void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        //_viewModel.ViewLoading();
    }


    private void SetupTeachingTips()
    {
        var teachingTipInitial = _viewModel._preferencesService.GetTeachingTip("TeachingTipBlank");
        var teachingTipNarr = _viewModel._preferencesService.GetTeachingTip("TeachingTipNarrDash");

        if (teachingTipInitial == null || !teachingTipInitial.IsShown)
        {
            _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipPortDash");
            _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrDash");

        }
        else if (teachingTipNarr != null && !teachingTipNarr.IsShown)
        {

            MyTeachingTipPort.IsOpen = true;
        }
    }

    private void OnGetItClickedPort(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipPort.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipPortDash");
        MyTeachingTipNarr.IsOpen = true;
        // Navigate to the new feature or provide additional information
    }
    private void OnGetItClickedNarr(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipNarr.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrDash");

        // Navigate to the new feature or provide additional information
    }

    private void OnDismissClickedPort(object sender, RoutedEventArgs e)
    {
        MyTeachingTipPort.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipPortDash");
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrDash");
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_viewModel != null)
            {
                _viewModel.Terminate();
                //_viewModel = null;
            }
        }
    }
}
