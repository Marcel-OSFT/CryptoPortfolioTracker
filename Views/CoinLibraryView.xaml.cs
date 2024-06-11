
using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public sealed partial class CoinLibraryView : Page
{

public readonly CoinLibraryViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static CoinLibraryView Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public CoinLibraryView(CoinLibraryViewModel viewModel)
    {
        Current = this;
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await _viewModel.Initialize();
        await _viewModel.RetrieveAllCoinData();
    }

    private void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.Terminate();
    }

}
