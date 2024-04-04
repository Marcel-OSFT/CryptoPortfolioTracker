
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace CryptoPortfolioTracker.Views
{
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
            InitAssetsListView();
           
        }

        #region MAIN methods or Tasks
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
       
        //public async void AccountsListView_ItemClicked(object sender, ItemClickEventArgs args)
        //{
        //    ListView listView = (ListView)sender;
        //    Account selectedAccount = listView.SelectedItem as Account;
        //    Account clickedAccount = args.ClickedItem as Account;

        //    //Clicked a new Account.... -> Resize Account ListView to small and Show Assets for this Account
        //    if (selectedAccount == null || selectedAccount != args.ClickedItem)
        //    {
        //        await _viewModel.ShowAssets(clickedAccount);

        //        ShowExtendedView(true);
        //        listView.UpdateLayout();
        //        listView.ScrollIntoView(clickedAccount);

        //    }
        //    //clicked the already selected Account.... ->
        //    if (selectedAccount != null && selectedAccount == args.ClickedItem)
        //    {
        //        // if Assets are not shown -> Decrease Account Listview to small and show assets for this account
        //        if (!isExtendedView)
        //        {
        //            await _viewModel.ShowAssets(clickedAccount);
        //            ShowExtendedView(true);
        //            listView.UpdateLayout();
        //            listView.ScrollIntoView(clickedAccount);
        //        }
        //        else //if Assets are shown -> close Assets List and resize Accounts Listview to full-size
        //        {
        //            //_viewModel.ListAssetTotals = null;
        //            _viewModel.ListAssetTotals.Clear();
        //            ShowExtendedView(false);
        //        }
        //    }
        //}

        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks
        
        public void Dispose()
        {

        }
        #endregion SUB methods or Tasks



    }

}
