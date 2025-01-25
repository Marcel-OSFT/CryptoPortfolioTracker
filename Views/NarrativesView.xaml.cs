using System;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class NarrativesView : Page, IDisposable
{
    public readonly NarrativesViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static NarrativesView Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public NarrativesView(NarrativesViewModel viewModel)
    {
        Current = this;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }
   
    private void InitAssetsListView()
    {
        // When assets are shown for an account, the general assets listview is used.
        MyNarrativesListViewControl.NarrativesListView.SelectedIndex = 0;
        MyAssetsListViewControl.AssetsListView.IsItemClickEnabled = false;
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
        await _viewModel.ViewLoading();
        InitAssetsListView();
    }

    private void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.Terminate();
        MyAssetsListViewControl.AssetsListView.DataContext = null;
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
