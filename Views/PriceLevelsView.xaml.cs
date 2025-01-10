using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CryptoPortfolioTracker.Views;

public partial class PriceLevelsView : Page, IDisposable
{

    public readonly PriceLevelsViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static PriceLevelsView Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public PriceLevelsView(PriceLevelsViewModel viewModel)
    {
        Current = this;
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await _viewModel.Initialize();
    }

    private void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.Terminate();
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
