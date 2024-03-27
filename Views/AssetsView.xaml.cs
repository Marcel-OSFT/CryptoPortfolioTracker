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
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Core;
using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Automation.Peers;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Views
{

    public partial class AssetsView : Page, IDisposable
    {
        public readonly AssetsViewModel _viewModel;
        public static AssetsView Current;
        private bool isExtendedView = false;

        public AssetsView(AssetsViewModel viewModel)// ** DI of viewModel into View
        {
            Current = this;
            _viewModel = viewModel;
            this.InitializeComponent();
            DataContext = _viewModel;
            //MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
        }


        #region MAIN methods or Tasks
        private async void Button_Click_AddTransactionDialog(object sender, RoutedEventArgs e)
        {
           await _viewModel.ShowTransactionDialog(DialogAction.Add);
        }
        private void AssetsView_loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel.ListAssetTotals.Count > 0)
            {
                ListView listView = (ListView)MyAssetsListViewControl.FindChild("AssetsListView");
                listView.SelectedIndex = 0;
                _viewModel.CalculateAssetsTotalValues();
            }
        }
        public async void AssetAccountsGridView_ItemClicked(object sender, ItemClickEventArgs args)
        {
            //new item clicked and selected?
            if (((GridView)sender).SelectedItem == null || ((GridView)sender).SelectedItem != args.ClickedItem)
            {
                
                await _viewModel.ShowAssetTransactions((args.ClickedItem) as AssetAccount);
            }
        }

        public async void AssetsListView_ItemClicked(object sender, ItemClickEventArgs args)
        {
            var listView = sender as ListView;
            var selectedAsset = listView.SelectedItem as AssetTotals;
            var clickedAsset = args.ClickedItem as AssetTotals;
            
            //new item clicked and selected?
            if (selectedAsset == null || selectedAsset != args.ClickedItem)
            {
                await _viewModel.ShowAccountsAndTransactions(clickedAsset);
                SetExtendedView(true);
                listView.UpdateLayout();
                listView.ScrollIntoView(clickedAsset);
            }
            //clicked already selected item
            if (selectedAsset != null && selectedAsset == args.ClickedItem)
            {
                if (!isExtendedView)
                {
                    await _viewModel.ShowAccountsAndTransactions(clickedAsset);
                    SetExtendedView(true);
                    listView.UpdateLayout();
                    listView.ScrollIntoView(clickedAsset);
                }
                else
                {
                    await _viewModel.ShowAccountsAndTransactions(null);
                    SetExtendedView(false);
                }
            }
        }
        //public async void AssetsListView_ItemClickOLD(object sender, ItemClickEventArgs args)
        //{
        //    ListView assetsListView = (ListView)sender;

        //    //new item clicked and selected
        //    if (assetsListView.SelectedItem == null || assetsListView.SelectedItem != args.ClickedItem)
        //    {
        //        
        //        (await _viewModel._assetService.GetAccountsByAsset((args.ClickedItem as AssetTotals).Coin.Id))
        //            .IfSucc(s => _viewModel.CreateListAssetAcounts(s));
        //        if (_viewModel.ListAssetAccounts.Count > 0)
        //        {
        //            GridView assetAccountsGridView = MyAssetAccountsGridViewControl.AssetAccountsGridView;
        //            assetAccountsGridView.SelectedIndex = 0;
        //            (await _viewModel._assetService.GetTransactionsByAsset((assetAccountsGridView.SelectedItem as AssetAccount).AssetId))
        //                .IfSucc(s => _viewModel.CreateListAssetTransactions(s));
        //        }
        //        SetViewToShowAccountsAndTransactions(true);
        //        assetsListView.UpdateLayout();
        //        assetsListView.ScrollIntoView(args.ClickedItem);

        //    }
        //    //clicked already selected item
        //    if (assetsListView.SelectedItem != null && assetsListView.SelectedItem == args.ClickedItem)
        //    {
        //        if (_viewModel.ListAssetAccounts == null)
        //        {
        //            (await _viewModel._assetService.GetAccountsByAsset((args.ClickedItem as AssetTotals).Coin.Id))
        //                .IfSucc(s => _viewModel.CreateListAssetAcounts(s));
        //            if (_viewModel.ListAssetAccounts.Count > 0)
        //            {
        //                GridView assetAccountsGridView = MyAssetAccountsGridViewControl.AssetAccountsGridView;
        //                assetAccountsGridView.SelectedIndex = 0;
        //                (await _viewModel._assetService.GetTransactionsByAsset((assetAccountsGridView.SelectedItem as AssetAccount).AssetId))
        //                    .IfSucc(s => _viewModel.CreateListAssetTransactions(s));
        //            }
        //            SetViewToShowAccountsAndTransactions(true);
        //            assetsListView.UpdateLayout();
        //            assetsListView.ScrollIntoView(args.ClickedItem);
        //        }
        //        else
        //        {
        //            _viewModel.ListAssetAccounts = null;
        //            _viewModel.ListAssetTransactions = null;
        //            SetViewToShowAccountsAndTransactions(false);
        //        }
        //    }
        //}
        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks
        private void SetExtendedView(bool isShown)
        {
            isExtendedView = isShown;
            if (isExtendedView)
            {
                MyAssetAccountsGridViewControl.AssetAccountsGridView.SelectedIndex = 0;

                AddTransactionButton.Visibility = Visibility.Collapsed;
                AssetsContent.RowDefinitions[0].Height = new GridLength(120, GridUnitType.Pixel);
                AssetsContent.RowDefinitions[1].Height = new GridLength(85, GridUnitType.Pixel);
                AssetsContent.RowDefinitions[2].Height = new GridLength(0.7, GridUnitType.Star);
            }
            else
            {
                AddTransactionButton.Visibility = Visibility.Visible;
                AssetsContent.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                AssetsContent.RowDefinitions[1].Height = new GridLength(0, GridUnitType.Star);
                AssetsContent.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Star);
            }
        }
        public void Dispose()
        {
            Debug.WriteLine("View Disposed");
        }
        #endregion SUB methods or Tasks



    }

}

