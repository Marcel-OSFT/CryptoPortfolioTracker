
using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class AccountsView : Page, IDisposable
{

    public readonly AccountsViewModel _viewModel;
    public static AccountsView Current;

    public AccountsView(AccountsViewModel viewModel)
    {
        Current = this;
        _viewModel = viewModel;
        this.InitializeComponent();
        DataContext = _viewModel;
    }

   
    private void InitAssetsListView()
    {
        // When assets are shown for an account, the general assets listview is used.
        MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
        if (_viewModel.ListAccounts != null && _viewModel.ListAccounts.Count > 0)
        {
            MyAccountsListViewControl.AccountsListView.SelectedIndex = 0;
            MyAssetsListViewControl.AssetsListView.IsItemClickEnabled = false;
        }
    }

    private async void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        await _viewModel.SetDataSource();
        InitAssetsListView();
    }

    public void Dispose()
    {
    }
}
