using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class AssetsView : Page, IDisposable
{
    public readonly AssetsViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AssetsView Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public AssetsView(AssetsViewModel viewModel)// ** DI of viewModel into View
    {
        Current = this;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await _viewModel.SetDataSource();
    }
    public void Dispose() // Implement IDisposable
    {
        GC.SuppressFinalize(this);
    }
}

