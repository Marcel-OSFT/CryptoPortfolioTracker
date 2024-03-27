
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.UI.Popups;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Views
{
    public partial class AccountsView : Page, IDisposable
    {

        public readonly AccountsViewModel _viewModel;
        public static AccountsView Current;
        private bool isExtendedView = false;
       
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
        private async void Button_Click_AddAccountDialog(object sender, RoutedEventArgs e)
        {
            await _viewModel.ShowAccountDialog(DialogAction.Add);
        }
        public async void AccountsListView_ItemClicked(object sender, ItemClickEventArgs args)
        {
            ListView listView = (ListView)sender;
            Account selectedAccount = listView.SelectedItem as Account; 
            Account clickedAccount = args.ClickedItem as Account;

            //Clicked a new Account.... -> Resize Account ListView to small and Show Assets for this Account
            if (selectedAccount == null || selectedAccount != args.ClickedItem)
            {
                await _viewModel.ShowAssets(clickedAccount);
                
                ShowExtendedView(true);
                listView.UpdateLayout();
                listView.ScrollIntoView(clickedAccount);

            }
            //clicked the already selected Account.... ->
            if (selectedAccount != null && selectedAccount == args.ClickedItem)
            {
                // if Assets are not shown -> Decrease Account Listview to small and show assets for this account
                if (!isExtendedView)
                {
                    await _viewModel.ShowAssets(clickedAccount);
                    ShowExtendedView(true);
                    listView.UpdateLayout();
                    listView.ScrollIntoView(clickedAccount);
                }
                else //if Assets are shown -> close Assets List and resize Accounts Listview to full-size
                {
                    _viewModel.ListAssetTotals = null;
                    ShowExtendedView(false);
                }
            }
        }
        
        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks
        private void ShowExtendedView(bool isShown)
        {
            isExtendedView = isShown;
            if (isExtendedView)
            {
                MyAssetsListViewControl.AssetsListView.SelectedIndex = 0;
                AddAccountButton.Visibility = Visibility.Collapsed;
                AccountsContent.RowDefinitions[0].Height = new GridLength(110, GridUnitType.Pixel);
                AccountsContent.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                AddAccountButton.Visibility = Visibility.Visible;
                AccountsContent.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                AccountsContent.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
            }
        }
        public void Dispose()
        {

        }
        #endregion SUB methods or Tasks



    }

}