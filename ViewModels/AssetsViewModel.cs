
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class AssetsViewModel : BaseViewModel
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AssetsViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public readonly IAssetService _assetService;
    public readonly IPriceUpdateService _priceUpdateBackgroundService;
    public readonly ITransactionService _transactionService;

    [ObservableProperty] private double totalAssetsValue;
    [ObservableProperty] private double totalAssetsCostBase;
    [ObservableProperty] private double totalAssetsPnLPerc;

    [ObservableProperty] private ObservableCollection<AssetTotals>? listAssetTotals;
    [ObservableProperty] private ObservableCollection<AssetAccount>? listAssetAccounts;
    [ObservableProperty] private ObservableCollection<Transaction>? listAssetTransactions;

    private AssetTotals? selectedAsset = null;
    private AssetAccount? selectedAccount = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowTransactionDialogToAddCommand))]
    private bool isExtendedView = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
    private bool isHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;

    public static List<CoinList>? coinListGecko;

    public AssetsViewModel(IAssetService assetService, IPriceUpdateService priceUpdateBackgroundService, ITransactionService transactionService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
        Current = this;
        _assetService = assetService;
        _priceUpdateBackgroundService = priceUpdateBackgroundService;
        _transactionService = transactionService;
        _priceUpdateBackgroundService.Start();
    }

    /// <summary>
    /// SetDataSource async task is called from the View_Loading event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task SetDataSource()
    {
        var result = (await _assetService.GetAssetTotals())
             .Match(Succ: s => CreateListAssetTotals(s), Fail: e => CreateListWithDummyAssetTotals());
    }

    [RelayCommand]
    public async Task AssetItemClicked(AssetTotals clickedAsset)
    {
        //new item clicked and selected ?
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
                await ShowAccountsAndTransactions();
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
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Adding");
            var dialog = new TransactionDialog(_transactionService, DialogAction.Add)
            {
                XamlRoot = AssetsView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (dialog.Exception != null)
            {
                throw dialog.Exception;
            }

            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Adding a new Transaction - {0}", dialog.transactionNew.Details.TransactionType);
                await (await _transactionService.AddTransaction(dialog.transactionNew))
                    .Match(Succ: newAsset => UpdateListAssetTotals(dialog.transactionNew),
                        Fail: async err =>
                        {
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
        AssetAccount? accountAffected = null;
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Editing");
            //*** editing a transaction also involves a change for an element in the ListAssetAccounts
#pragma warning disable CS8604 // Possible null reference argument.
            accountAffected = ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
#pragma warning restore CS8604 // Possible null reference argument.

            var dialog = new TransactionDialog(_transactionService, DialogAction.Edit, transaction)
            {
                XamlRoot = AssetsView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (dialog.Exception != null)
            {
                throw dialog.Exception;
            }

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
                            Fail: async err =>
                            {
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
        var loc = Localizer.Get();
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
                //*** editing a transaction also involves a change for an element in the ListAssetAccounts
#pragma warning disable CS8604 // Possible null reference argument.
                var accountAffected = ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
#pragma warning restore CS8604 // Possible null reference argument.

                await (await _transactionService.DeleteTransaction(transaction, accountAffected))
                         .Match(Succ: async s =>
                         {
                             await UpdateListAssetTotals(transaction);
                             await UpdateListAssetAccount(accountAffected);
                             await RemoveFromListAssetTransactions(transaction);
                         },
                            Fail: async err =>
                            {
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

    public async Task ShowAccountsAndTransactions(AssetTotals? clickedAsset = null)
    {
        if (clickedAsset != null)
        {
            (await _assetService.GetAccountsByAsset(clickedAsset.Coin.Id))
                                .IfSucc(s => CreateListAssetAcounts(s));
            if (ListAssetAccounts is not null && ListAssetAccounts.Count > 0)
            {
                var firstAccount = ListAssetAccounts.First();
                (await _assetService.GetTransactionsByAsset(firstAccount.AssetId))
                    .IfSucc(s => CreateListAssetTransactions(s));
            }
        }
        else
        {
            ListAssetAccounts?.Clear();
            ListAssetTransactions?.Clear();
        }

    }
    public async Task ClearAccountsAndTransactions(AssetTotals clickedAsset)
    {
        (await _assetService.GetAccountsByAsset(clickedAsset.Coin.Id))
                .IfSucc(s => CreateListAssetAcounts(s));
        if (ListAssetAccounts is not null && ListAssetAccounts.Count > 0)
        {
            var firstAccount = ListAssetAccounts.First();
            (await _assetService.GetTransactionsByAsset(firstAccount.AssetId))
                .IfSucc(s => CreateListAssetTransactions(s));
        }
    }

    [RelayCommand]
    public void HideZeroBalances(bool param)
    {
        if (ListAssetTotals == null)
        {
            return;
        }

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

    public async Task UpdateListAssetAccount(AssetAccount accountAffected)
    {
        if (ListAssetAccounts is null)
        {
            return;
        } (await _assetService.GetAccountByAsset(accountAffected.AssetId))
            .IfSucc(s =>
            {
                var index = -1;
                
                for (var i = 0; i < ListAssetAccounts.Count; i++)
                {
                    if ( ListAssetAccounts[i].Name == accountAffected.Name)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    return;
                }

                if (s != null && s.Name != string.Empty)
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
        if (ListAssetTotals is null)
        {
            return;
        }
        //for updating purpose of the View, the affected elements of the data source List has to be updated
        //*** First retrieve the coin(s) (max 2) affected by the transaction
        var coinsAffected = transaction.Mutations.Select(x => x.Asset.Coin).Distinct().ToList();

        // Check if one isn't in the assetsList yet, if so then add it.
        foreach (var coin in coinsAffected)
        {
            var assetAffected = ListAssetTotals.Where(x => x.Coin.Id == coin.Id).SingleOrDefault();
            if (assetAffected != null)
            {
                var index = -1;
                for (var i = 0; i < ListAssetTotals.Count; i++)
                {
                    if (ListAssetTotals[i].Coin.Id == assetAffected.Coin.Id)
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                {
                    var editedAT = (await _assetService.GetAssetTotalsByCoin(coin)).Match(Succ: s => s, Fail: err => new AssetTotals());
                    if (editedAT.Coin is not null)
                    {
                        ListAssetTotals[index] = editedAT;
                    }
                }
            }
            else //assetAffected == null
            {
                assetAffected = new AssetTotals();

                (await _assetService.GetAssetTotalsByCoin(coin)).IfSucc(s =>
                {
                    assetAffected = s;
                    ListAssetTotals.Add(assetAffected);
                });
            }
        }
    }
    public Task UpdateListAssetTransaction(Transaction transactionNew, Transaction transactionToEdit)
    {
        if (ListAssetTransactions is null)
        {
            return Task.CompletedTask;
        }

        var index = -1;
        for (var i = 0; i < ListAssetTransactions.Count; i++)
        {
            if (ListAssetTransactions[i].Id == transactionToEdit.Id)
            {
                index = i;
                break;
            }
        }
        if (index >= 0)
        {
            ListAssetTransactions[index] = transactionNew;
        }

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
        //TODO error caused below => look into XAML
       // return true;

        ListAssetTransactions = new ObservableCollection<Transaction>(list);
        var test = true;
        return ListAssetTransactions.Any();
    }
    private bool CreateListWithDummyAssetTotals()
    {
        var dummyCoin = new Coin()
        {
            Name = "EXCEPTIONAL ERROR",
            Symbol = "EXCEPTIONAL ERROR"
        };
        var dummyAssetTotals = new AssetTotals()
        {
            Coin = dummyCoin
        };

        ListAssetTotals = new ObservableCollection<AssetTotals>
        {
            dummyAssetTotals
        };
        return ListAssetTotals.Any();
    }
    public Task<bool> RemoveFromListAssetTransactions(Transaction deletedTransaction)
    {
        if (ListAssetTransactions is null) { return Task.FromResult(false); }
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
        Logger.Information("CalculatingTotals {0}", TotalAssetsValue);
    }
}

