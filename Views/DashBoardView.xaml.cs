using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Controls;

namespace CryptoPortfolioTracker.Views;

public sealed partial class DashboardView : Page
{
    private readonly DashboardViewModel _viewModel;
    public static DashboardView Current;

    public DashboardView(DashboardViewModel viewModel)
    {
        _viewModel = viewModel;
        Current = this;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        // await _viewModel.Initialize();
    }

    private void View_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }

    private async void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
       // _viewModel.Terminate();
    }

}
