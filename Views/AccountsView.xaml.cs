
using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class AccountsView : Page, IDisposable
{

    public readonly AccountsViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AccountsView Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public AccountsView(AccountsViewModel viewModel)
    {
        Current = this;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }
   
    private void InitAssetsListView()
    {
        // When assets are shown for an account, the general assets listview is used.
        //if (_viewModel.ListAccounts != null && _viewModel.ListAccounts.Count > 0)
        //{
            MyAccountsListViewControl.AccountsListView.SelectedIndex = 0;
            MyAssetsListViewControl.AssetsListView.IsItemClickEnabled = false;
        //}
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;

        await _viewModel.Initialize();
        InitAssetsListView();
    }

    private void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Current = null;

        _viewModel.Terminate();
        MyAssetsListViewControl.AssetsListView.DataContext = null;
    }

    public void Dispose()
    {
        Current = null;
    }
     
 }
