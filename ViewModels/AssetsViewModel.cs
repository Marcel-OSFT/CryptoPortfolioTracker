
#region Using
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.UI.Core;
using Windows.UI.Popups;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using LanguageExt;
using System.Collections;
using LanguageExt.Common;
using System.Transactions;
using WinRT;
using Windows.UI;
#endregion Using

namespace CryptoPortfolioTracker.ViewModels
{
    public sealed partial class AssetsViewModel : BaseViewModel, IDisposable
    {
        #region Fields related to the MVVM design pattern
        public static AssetsViewModel Current;
        #endregion Fields related to the MVVM design pattern

        #region instances related to Services
        public readonly IAssetService _assetService;
        public readonly IPriceUpdateBackgroundService _priceUpdateBackgroundService;
        private readonly ITransactionService _transactionService;

        #endregion instances related to Services

        #region Fields and Proporties for DataBinding with the View
        private double totalAssetsValue;
        public double TotalAssetsValue
        {
            get { return totalAssetsValue; }
            set
            {
                if (totalAssetsValue == value) return;
                totalAssetsValue = value;
                OnPropertyChanged(nameof(TotalAssetsValue));
            }
        }
        private double totalAssetsCostBase;
        public double TotalAssetsCostBase
        {
            get { return totalAssetsCostBase; }
            set
            {
                if (totalAssetsCostBase == value) return;
                totalAssetsCostBase = value;
                OnPropertyChanged(nameof(TotalAssetsCostBase));
            }
        }
        private double totalAssetsPnLPerc;
        public double TotalAssetsPnLPerc
        {
            get { return totalAssetsPnLPerc; }
            set
            {
                if (totalAssetsPnLPerc == value) return;
                totalAssetsPnLPerc = value;
                OnPropertyChanged(nameof(TotalAssetsPnLPerc));
            }
        }

        private ObservableCollection<AssetTotals> listAssetTotals;
        public ObservableCollection<AssetTotals> ListAssetTotals
        {
            get { return listAssetTotals; }
            set
            {
                if (listAssetTotals == value) return;
                listAssetTotals = value;
                OnPropertyChanged(nameof(ListAssetTotals));
            }
        }
        private ObservableCollection<AssetAccount> listAssetAccounts;
        public ObservableCollection<AssetAccount> ListAssetAccounts
        {
            get { return listAssetAccounts; }
            set
            {
                if (listAssetAccounts == value) return;
                listAssetAccounts = value;
                OnPropertyChanged(nameof(ListAssetAccounts));
            }
        }
        private ObservableCollection<AssetTransaction> listAssetTransactions;
        public ObservableCollection<AssetTransaction> ListAssetTransactions
        {
            get { return listAssetTransactions; }
            set
            {
                if (listAssetTransactions == value) return;
                listAssetTransactions = value;
                OnPropertyChanged(nameof(ListAssetTransactions));
            }
        }

        #endregion variables and proporties for DataBinding with the View
        


        //Constructor
        public AssetsViewModel(IAssetService assetService, IPriceUpdateBackgroundService priceUpdateBackgroundService, ITransactionService transactionService)
        {
            Current = this;
            _assetService = assetService;
            SetDataSource();

            _priceUpdateBackgroundService = priceUpdateBackgroundService;
            _transactionService = transactionService;
            _priceUpdateBackgroundService.Start();
            
        }

        #region MAIN methods or Tasks
        private async Task SetDataSource()
        {
           (await _assetService.GetAssetTotals())
                .Match(Succ: s => CreateListAssetTotals(s), Fail: e => CreateListWithDummyAssetTotals());
        }
        public async Task ShowTransactionDialog(DialogAction dialogAction, int transactionId = 0)
        {
            //*** ListAssetTransactions=null soms na ICommandEdit while tr.Id != 0
            AssetTransaction transactionToEdit = null;
            AssetAccount accountAffected = null;
            
            try
            {
                if (dialogAction == DialogAction.Edit)
                {
                    transactionToEdit = ListAssetTransactions.Where(t => t.Id == transactionId).Single();
                    //*** editing a transaction also involves a change for an element in the ListAssetAccounts
                    accountAffected = ListAssetAccounts.Where(t => t.AssetId == transactionToEdit.RequestedAsset.Id).Single();
                    
                }
                var dialog = new TransactionDialog(_transactionService, dialogAction, transactionToEdit);
                dialog.XamlRoot = AssetsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (dialog.Exception != null) throw dialog.Exception;
                if (result == ContentDialogResult.Primary)
                {
                    
                    if (dialogAction == DialogAction.Add)
                    {
                        await (await _transactionService.AddTransaction(dialog.transactionNew))
                         .Match(Succ: newAsset => UpdateListAssetTotals(dialog.transactionNew),
                             Fail: async err => await ShowMessageBox("Adding transaction failed (" + err.Message + ")"));

                    }
                    else if (dialogAction == DialogAction.Edit)
                    {
                       await (await _transactionService.EditTransaction(dialog.transactionNew, transactionToEdit))
                            .Match(Succ: async newAsset => {
                                await UpdateListAssetTotals(dialog.transactionNew);
                                await UpdateListAssetAccount(accountAffected);
                            },
                                Fail: async err => await ShowMessageBox("Updating transaction failed (" + err.Message + ")"));
                    }
                    CalculateAssetsTotalValues();
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Failure on showing Transaction Dialog (" + ex.Message + ")");
            }
        }
        public async Task DeleteTransaction(int transactionId)
        {
            try
            {
                MsgBoxDialog dialog = new MsgBoxDialog("Are you sure you want to delete this transaction? Select CONFIRM to delete and revert this transaction");
                dialog.XamlRoot = AssetsView.Current.XamlRoot;
                dialog.Title = "Delete Transaction";
                var result = await dialog.ShowAsync();
                
                if (result == ContentDialogResult.Primary)
                {
                    var transactionToDelete = ListAssetTransactions.Where(t => t.Id == transactionId).Single();
                    //*** editing a transaction also involves a change for an element in the ListAssetAccounts
                    var accountAffected = ListAssetAccounts.Where(t => t.AssetId == transactionToDelete.RequestedAsset.Id).Single();

                    await (await _transactionService.DeleteTransaction(transactionToDelete, accountAffected))
                             .Match(Succ: async s => {
                                 await UpdateListAssetTotals(transactionToDelete);
                                 await RemoveFromListAssetTransactions(transactionToDelete);
                                 },
                                    Fail: err => ShowMessageBox("Deleting transaction failed (" + err.Message + ")"));
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Deleting transaction failed (" + ex.Message + ")");
            }
        }
        public async Task ShowAssetTransactions(AssetAccount clickedAccount)
        {
            (await _assetService.GetTransactionsByAsset(clickedAccount.AssetId))
                .IfSucc(s => CreateListAssetTransactions(s));
        }

        public async Task ShowAccountsAndTransactions(AssetTotals clickedAsset = null)
        {
            if (clickedAsset != null)
            {
                (await _assetService.GetAccountsByAsset(clickedAsset.Coin.Id))
                                    .IfSucc(s => CreateListAssetAcounts(s));
                if (ListAssetAccounts.Count > 0)
                {
                    var firstAccount = ListAssetAccounts.FirstOrDefault();
                    (await _assetService.GetTransactionsByAsset(firstAccount.AssetId))
                        .IfSucc(s => CreateListAssetTransactions(s));
                }
            }
            else
            {
                ListAssetAccounts = null;
                ListAssetTransactions = null;
            }
            
        }
        public async Task ClearAccountsAndTransactions(AssetTotals clickedAsset)
        {
            (await _assetService.GetAccountsByAsset(clickedAsset.Coin.Id))
                    .IfSucc(s => CreateListAssetAcounts(s));
            if (ListAssetAccounts.Count > 0)
            {
                var firstAccount = ListAssetAccounts.FirstOrDefault();
                (await _assetService.GetTransactionsByAsset(firstAccount.AssetId))
                    .IfSucc(s => CreateListAssetTransactions(s));
            }
        }



        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks
      

        public async Task UpdateListAssetAccount(AssetAccount accountAffected)
        {
            (await _assetService.GetAccountByAsset(accountAffected.AssetId))
                .IfSucc(s => {

                   var index = ListAssetAccounts.IndexOf(accountAffected);
                    ListAssetAccounts[index] = s;
                });
        }



        public async Task UpdateListAssetTotals(AssetTransaction transaction)
        {
            Debug.WriteLine("");

            //for updating purpose of the View, the affected elements of the data source List has to be updated
            //*** First retrieve the coin(s) (max 2) affected by the transaction
            var coinsAffected = transaction.Mutations.Select(x => x.Asset.Coin).Distinct().ToList();
            

            // Check if one isn't in the assetsList yet, if so then add it.
            foreach (var coin in coinsAffected)
            {
                var assetAffected = (AssetTotals)ListAssetTotals.Where(x => x.Coin == coin).SingleOrDefault();
                var index = ListAssetTotals.IndexOf(assetAffected);
                
                if (assetAffected == null)
                {
                    assetAffected = new AssetTotals();

                    (await _assetService.GetAssetTotalsByCoin(coin)).IfSucc(s =>
                    {
                        assetAffected = s;
                        ListAssetTotals.Add(assetAffected);
                    });
                }
                else
                {
                    assetAffected = (await _assetService.GetAssetTotalsByCoin(coin)).Match(Succ: s => s, Fail: err => null);
                    ListAssetTotals[index] = assetAffected;
                    await Task.Delay(500);
                }
            }
        }
        public bool CreateListAssetTotals(List<AssetTotals> list)
        {
            ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue));
            return ListAssetTotals.Any();
        }
        public bool CreateListAssetAcounts(List<AssetAccount> list)
        {
            ListAssetAccounts = new ObservableCollection<AssetAccount>(list.OrderByDescending(x => x.Qty));
            return ListAssetAccounts.Any();
        }
        public bool CreateListAssetTransactions(List<AssetTransaction> list)
        {
            ListAssetTransactions = new ObservableCollection<AssetTransaction>(list);
            return ListAssetTransactions.Any();
        }
        private bool CreateListWithDummyAssetTotals()
        {
            Coin dummyCoin = new Coin()
            { 
                Name = "EXCEPTIONAL ERROR",
                Symbol = "EXCEPTIONAL ERROR"
            };
            var dummyAssetTotals = new AssetTotals()
            {
                Coin = dummyCoin
            };

            ListAssetTotals = new ObservableCollection<AssetTotals>();
            ListAssetTotals.Add(dummyAssetTotals);
            return ListAssetTotals.Any();
        }
        public Task<bool> RemoveFromListAssetTransactions(AssetTransaction deletedTransaction)
        {
            var transactionToUpdate = ListAssetTransactions.Where(x => x.Id == deletedTransaction.Id).Single();
            ListAssetTransactions.Remove(deletedTransaction);
           
            return Task.FromResult(true);
        }
        
        public void CalculateAssetsTotalValues()
        {
            if (ListAssetTotals != null && ListAssetTotals.Count > 0 && ListAssetTotals[0].Coin.Symbol != "EXCEPTIONAL ERROR")
            {
                TotalAssetsValue = ListAssetTotals.Sum(x => x.MarketValue);
                TotalAssetsCostBase = ListAssetTotals.Sum(x => x.CostBase);
                TotalAssetsPnLPerc = 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase;
            }
        }
        private async Task ShowMessageBox(string message, string primaryButtonText = "OK", string closeButtonText = "Close")
        {
            var dlg = new MsgBoxDialog(message);
            dlg.XamlRoot = AssetsView.Current.XamlRoot;
            dlg.PrimaryButtonText = primaryButtonText;
            dlg.CloseButtonText = closeButtonText;
            await dlg.ShowAsync();
        }

        public void Dispose()
        {
        }
        #endregion SUB methods or Tasks

    }

}

