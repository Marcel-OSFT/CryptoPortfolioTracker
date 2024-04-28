
using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public sealed partial class CoinLibraryView : Page, IDisposable
{
    public readonly CoinLibraryViewModel _viewModel;
    public static CoinLibraryView Current;

    public CoinLibraryView(CoinLibraryViewModel viewModel)
    {
        Current = this;
        this.InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await _viewModel.SetDataSource();
       await _viewModel.RetrieveAllCoinData();
    }

    public void Dispose()
    {

    }

}
