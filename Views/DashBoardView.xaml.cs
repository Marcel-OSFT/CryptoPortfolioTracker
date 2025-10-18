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
        PortfolioPie.pieHeader.Tag = "Portfolio";
        AccountsPie.pieHeader.Tag = "Accounts";
        NarrativesPie.pieHeader.Tag = "Narratives";
    }

    private async void View_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.ViewLoading();
    }

    private void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        //_viewModel.ViewLoading();
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
