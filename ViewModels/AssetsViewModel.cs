
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
using LanguageExt.Pipes;
using LanguageExt.TypeClasses;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using WinRT;
using WinUI3Localizer;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
#endregion Using

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class AssetsViewModel : BaseViewModel, IDisposable
{
    #region Fields related to the MVVM design pattern
    public static AssetsViewModel Current;
    #endregion Fields related to the MVVM design pattern

    #region instances related to Services
    public readonly IAssetService _assetService;
    public readonly IPriceUpdateService _priceUpdateBackgroundService;
    public readonly ITransactionService _transactionService;

    #endregion instances related to Services

    #region Fields and Proporties for DataBinding with the View
    
    [ObservableProperty] double totalAssetsValue;
    [ObservableProperty] double totalAssetsCostBase;
    [ObservableProperty] double totalAssetsPnLPerc;

    

    [ObservableProperty] ObservableCollection<AssetTotals> listAssetTotals;
    [ObservableProperty] ObservableCollection<AssetAccount> listAssetAccounts;
    [ObservableProperty] ObservableCollection<Transaction> listAssetTransactions;

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
    public AssetsViewModel(IAssetService assetService, IPriceUpdateService priceUpdateBackgroundService, ITransactionService transactionService)
    {
        //Logger = Log.Logger.ForContext<AssetsViewModel>();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
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
        App.isBusy = true;
        ILocalizer loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Adding");
            var dialog = new TransactionDialog(_transactionService, DialogAction.Add);
            dialog.XamlRoot = AssetsView.Current.XamlRoot;
            var result = await dialog.ShowAsync();

            if (dialog.Exception != null) throw dialog.Exception;
            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Adding a new Transaction - {0}", dialog.transactionNew.Details.TransactionType);
                await (await _transactionService.AddTransaction(dialog.transactionNew))
                    .Match(Succ: newAsset => UpdateListAssetTotals(dialog.transactionNew),
                        Fail: async err => {
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_TransactionAddFailed_Title"),
                            err.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Adding Transaction failed");
                        });
                CalculateAssetsTotalValues();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Transaction Dialog");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_TransactionDialogFailed_Title"), 
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }
    private bool CanShowTransactionDialogToAdd()
    {
        return !IsExtendedView;
    }

    [RelayCommand]
    public async Task ShowTransactionDialogToEdit(Transaction transaction)
    {
        App.isBusy = true;

       // Transaction transactionToEdit = null;
        AssetAccount accountAffected = null;
        ILocalizer loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Editing");

            //transactionToEdit = ListAssetTransactions.Where(t => t.Id == transactionId).Single();
            //*** editing a transaction also involves a change for an element in the ListAssetAccounts
            accountAffected = ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();

            var dialog = new TransactionDialog(_transactionService, DialogAction.Edit, transaction);
            dialog.XamlRoot = AssetsView.Current.XamlRoot;
            var result = await dialog.ShowAsync();

            if (dialog.Exception != null) throw dialog.Exception;
            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Editing Transaction ({0}) - {1}", transaction.Id, transaction.Details.TransactionType);
                await (await _transactionService.EditTransaction(dialog.transactionNew, transaction))
                        .Match(Succ: async newAsset =>
                        {
                            await UpdateListAssetTotals(dialog.transactionNew);
                            await UpdateListAssetAccount(accountAffected);
                            await UpdateListAssetTransaction(dialog.transactionNew, transaction);

                        },
                            Fail: async err => {
                                await ShowMessageDialog(
                                loc.GetLocalizedString("Messages_TransactionUpdateFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                                Logger.Error(err, "Editing Transaction failed");

                            });
                CalculateAssetsTotalValues();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Transaction Dialog");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_TransactionDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }

    [RelayCommand]
    public async Task DeleteTransaction(Transaction transaction)
    {
        App.isBusy = true;
        ILocalizer loc = Localizer.Get();
        try
        {
            Logger.Information("Deletion Request for Transaction ({0}) - {1}", transaction.Id, transaction.Details.TransactionType);

            var dlgResult = await ShowMessageDialog(
                loc.GetLocalizedString("Messages_TransactionDelete_Title"),
                loc.GetLocalizedString("Messages_TransactionDelete_Msg"),
                loc.GetLocalizedString("Common_ConfirmButton"),
                loc.GetLocalizedString("Common_CancelButton"));

            if (dlgResult == ContentDialogResult.Primary)
            {
                Logger.Information("Deleting Transaction");

                // var transactionToDelete = ListAssetTransactions.Where(t => t.Id == transactionId).Single();
                //*** editing a transaction also involves a change for an element in the ListAssetAccounts
                var accountAffected = ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
                
                await (await _transactionService.DeleteTransaction(transaction, accountAffected))
                         .Match(Succ: async s =>
                         {
                             await UpdateListAssetTotals(transaction);
                             await UpdateListAssetAccount(accountAffected);
                             await RemoveFromListAssetTransactions(transaction);
                         },
                            Fail: async err => {
                                await ShowMessageDialog(
                                    loc.GetLocalizedString("Messages_TransactionDeleteFailed_Title"),
                                    err.Message,
                                    loc.GetLocalizedString("Common_CloseButton"));
                                Logger.Error(err, "Deleting Transaction failed");

                            });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Deleting Transaction failed");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_TransactionDeleteFailed_Title"),  
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
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
            ListAssetAccounts.Clear();
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
                int index = -1;
                for (var i = 0; i < ListAssetAccounts.Count; i++)
                {
                    if (ListAssetAccounts[i].Name == accountAffected.Name)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1) return;
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

    public async Task UpdateListAssetTotals(Transaction transaction)
    {
        Debug.WriteLine("");

        //for updating purpose of the View, the affected elements of the data source List has to be updated
        //*** First retrieve the coin(s) (max 2) affected by the transaction
        var coinsAffected = transaction.Mutations.Select(x => x.Asset.Coin).Distinct().ToList();


        // Check if one isn't in the assetsList yet, if so then add it.
        foreach (var coin in coinsAffected)
        {
            var assetAffected = (AssetTotals)ListAssetTotals.Where(x => x.Coin.Id == coin.Id).SingleOrDefault();

            int index = -1;
            for (var i = 0; i < ListAssetTotals.Count; i++)
            {
                if (ListAssetTotals[i].Coin.Id == assetAffected.Coin.Id)
                {
                    index = i;
                    break;
                }
            }

            if (assetAffected == null)
            {
                assetAffected = new AssetTotals();

                (await _assetService.GetAssetTotalsByCoin(coin)).IfSucc(s =>
                {
                    assetAffected = s;
                    ListAssetTotals.Add(assetAffected);
                    Debug.WriteLine("added " + assetAffected.Coin.Name );
                });
            }
            else if (index >= 0)
            {
                var editedAT = (await _assetService.GetAssetTotalsByCoin(coin)).Match(Succ: s => s, Fail: err => null);
                ListAssetTotals[index] = editedAT;
                Debug.WriteLine("updated " + editedAT.Coin.Name);
            }
        }
    }
    public Task UpdateListAssetTransaction(Transaction transactionNew, Transaction transactionToEdit)
    {
        int index = -1;
        for (var i = 0; i < ListAssetTransactions.Count; i++)
        {
            if (ListAssetTransactions[i].Id == transactionToEdit.Id)
            {
                index = i;
                break;
            }
        }
        if (index >= 0) ListAssetTransactions[index] = transactionNew;
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
    public bool CreateListAssetTransactions(List<Transaction> list)
    {
        ListAssetTransactions = new ObservableCollection<Transaction>(list);
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
    public Task<bool> RemoveFromListAssetTransactions(Transaction deletedTransaction)
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
    

   
    #endregion SUB methods or Tasks

}

