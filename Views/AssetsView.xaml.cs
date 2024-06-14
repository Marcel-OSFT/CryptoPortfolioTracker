using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class AssetsView : Page
{
    public AssetsViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AssetsView Current;
    private bool disposedValue;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public AssetsView(AssetsViewModel viewModel)// ** DI of viewModel into View
    {
        Current = this;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
        await _viewModel.Initialize();
    }

    private void View_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (MyAssetsListViewControl.AssetsListView.Items.Count > 0)
        {
            MyAssetsListViewControl.AssetsListView.SelectedIndex = 0;
        }
        else
        {
            MyAssetsListViewControl.AssetsListView.SelectedIndex = -1;
        }
    }

    private async void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.Terminate();
        MyAssetsListViewControl.AssetsListView.DataContext = null;
    }


}

