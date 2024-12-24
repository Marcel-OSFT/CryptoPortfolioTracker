using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Views;
[ObservableObject]
public sealed partial class DashboardView : Page
{
   private readonly DashboardViewModel _viewModel;
    public static DashboardView Current;
   

    public DashboardView(DashboardViewModel dashboardVm)
    {
        Current = this;
        
        InitializeComponent();
        _viewModel = dashboardVm;
        DataContext = _viewModel;
        
        Portfolio.pieHeader.Tag = "Portfolio";
        Accounts.pieHeader.Tag = "Accounts";
    }

    private void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        
        _viewModel.Initialize();
    }
}
