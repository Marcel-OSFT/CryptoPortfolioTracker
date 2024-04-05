
#region Using
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinRT;
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
        public readonly ITransactionService _transactionService;

        #endregion instances related to Services

        #region Fields and Proporties for DataBinding with the View
        
        [ObservableProperty] double totalAssetsValue;
        [ObservableProperty] double totalAssetsCostBase;
        [ObservableProperty] double totalAssetsPnLPerc;

        [ObservableProperty] ObservableCollection<AssetTotals> listAssetTotals;
        [ObservableProperty] ObservableCollection<AssetAccount> listAssetAccounts;
        [ObservableProperty] ObservableCollection<AssetTransaction> listAssetTransactions;

        private AssetTotals selectedAsset = null;
        private AssetAccount selectedAccount = null;
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowTransactionDialogToAddCommand))]
        private bool isExtendedView = false;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
        bool isHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;


        public static List<CoinList> coinListGecko;
       

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


        [RelayCommand]
        public async Task AssetItemClicked(AssetTotals clickedAsset)
        {
            //new item clicked and selected?
            if (selectedAsset == null || selectedAsset != clickedAsset)
            {
                await ShowAccountsAndTransactions(clickedAsset);
                IsExtendedView = true;
            }
            //clicked already selected item
            if (selectedAsset != null && selectedAsset == clickedAsset)
            {
                if (!IsExtendedView)
                {
                    await ShowAccountsAndTransactions(clickedAsset);
                    IsExtendedView = true;
                }
                else
                {
                    await ShowAccountsAndTransactions(null);
                    IsExtendedView = false;
                }
            }
            selectedAsset = clickedAsset;
        }

        [RelayCommand]
        public async Task AccountItemClicked(AssetAccount clickedAccount)
        {
            //new item clicked and selected?
            if (selectedAccount == null || selectedAccount != clickedAccount)
            {

                await ShowAssetTransactions(clickedAccount);
            }
            selectedAccount = clickedAccount;
        }


        [RelayCommand(CanExecute = nameof(CanShowTransactionDialogToAdd))]
        public async Task ShowTransactionDialogToAdd()
        {
            try
            {
                var dialog = new TransactionDialog(_transactionService, DialogAction.Add);
                dialog.XamlRoot = AssetsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (dialog.Exception != null) throw dialog.Exception;
                if (result == ContentDialogResult.Primary)
                {
                    await (await _transactionService.AddTransaction(dialog.transactionNew))
                        .Match(Succ: newAsset => UpdateListAssetTotals(dialog.transactionNew),
                            Fail: async err => await ShowMessageBox("Adding transaction failed", err.Message));
                    CalculateAssetsTotalValues();
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Transaction Dialog Failure", ex.Message);
            }
        }
        private bool CanShowTransactionDialogToAdd()
        {
            return !IsExtendedView;
        }

        [RelayCommand]
        public async Task ShowTransactionDialogToEdit(int transactionId)
        {
            AssetTransaction transactionToEdit = null;
            AssetAccount accountAffected = null;

            try
            {
                transactionToEdit = ListAssetTransactions.Where(t => t.Id == transactionId).Single();
                //*** editing a transaction also involves a change for an element in the ListAssetAccounts
                accountAffected = ListAssetAccounts.Where(t => t.AssetId == transactionToEdit.RequestedAsset.Id).Single();

                var dialog = new TransactionDialog(_transactionService, DialogAction.Edit, transactionToEdit);
                dialog.XamlRoot = AssetsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (dialog.Exception != null) throw dialog.Exception;
                if (result == ContentDialogResult.Primary)
                {
                    await (await _transactionService.EditTransaction(dialog.transactionNew, transactionToEdit))
                            .Match(Succ: async newAsset =>
                            {
                                await UpdateListAssetTotals(dialog.transactionNew);
                                await UpdateListAssetAccount(accountAffected);
                                await UpdateListAssetTransaction(dialog.transactionNew, transactionToEdit);

                            },
                                Fail: async err => await ShowMessageBox("Updating transaction failed", err.Message));
                    CalculateAssetsTotalValues();
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Transaction Dialog Failure", ex.Message);
            }
        }

        [RelayCommand]
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
                             .Match(Succ: async s =>
                             {
                                 await UpdateListAssetTotals(transactionToDelete);
                                 await UpdateListAssetAccount(accountAffected);
                                 await RemoveFromListAssetTransactions(transactionToDelete);
                             },
                                    Fail: err => ShowMessageBox("Deleting transaction failed", err.Message));
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Deleting transaction failed", ex.Message);
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
                //ListAssetAccounts = null;
                ListAssetAccounts.Clear();
                //ListAssetTransactions = null;
                ListAssetTransactions.Clear();
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

        [RelayCommand]
        public void HideZeroBalances(bool param)
        {
            if (ListAssetTotals == null) return;
            if (param)
            {
                var itemsToHide = ListAssetTotals.Where(x => x.MarketValue <= 0).ToList();
                foreach (var item in itemsToHide)
                {
                    item.IsHidden = true; ;
                }
            }
            else
            {
                foreach (var item in ListAssetTotals)
                {
                    item.IsHidden = false; ;
                }
            }
        }

        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks


        public async Task UpdateListAssetAccount(AssetAccount accountAffected)
        {
            (await _assetService.GetAccountByAsset(accountAffected.AssetId))
                .IfSucc(s =>
                {
                    var index = ListAssetAccounts.IndexOf(accountAffected);
                    if (s != null)
                    {
                        ListAssetAccounts[index] = s;
                    }
                    else
                    {
                        ListAssetAccounts.RemoveAt(index);
                    }
                });
        }

        public async Task UpdateListAssetTotals(AssetTransaction transaction)
        {
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
                    var editedAT = (await _assetService.GetAssetTotalsByCoin(coin)).Match(Succ: s => s, Fail: err => null);
                    ListAssetTotals[index] = editedAT;

                }
            }
        }
        public Task UpdateListAssetTransaction(AssetTransaction transactionNew, AssetTransaction transactionToEdit)
        {
            var index = ListAssetTransactions.IndexOf(transactionToEdit);
            ListAssetTransactions[index] = transactionNew;
            return Task.CompletedTask;
        }
        public bool CreateListAssetTotals(List<AssetTotals> list)
        {
            ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue));
            HideZeroBalances(IsHidingZeroBalances);
            CalculateAssetsTotalValues();
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
        private async Task ShowMessageBox(string title, string message)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                Title = title,
                XamlRoot = AssetsView.Current.XamlRoot,
                Content = message,
                PrimaryButtonText = "OK"
            };
            await errorDialog.ShowAsync();
        }

        public void Dispose()
        {
        }
        #endregion SUB methods or Tasks

    }

}

