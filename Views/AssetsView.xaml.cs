using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class AssetsView : Page, IDisposable
{
    public readonly AssetsViewModel _viewModel;
    public static AssetsView Current;

    public AssetsView(AssetsViewModel viewModel)// ** DI of viewModel into View
    {
        Current = this;
        _viewModel = viewModel;
        this.InitializeComponent();
        DataContext = _viewModel;
        MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await _viewModel.SetDataSource();
    }


    public void Dispose()
    {
    }
}

