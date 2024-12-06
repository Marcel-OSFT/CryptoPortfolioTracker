
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using LanguageExt;
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

    public IAssetService _assetService {  get; private set; }
    public ITransactionService _transactionService { get; private set; }
    public IAccountService _accountService { get; private set; }

    public readonly IGraphService _graphService;
    private readonly IPreferencesService _preferencesService;

   // [ObservableProperty] private double totalAssetsValue;
   // [ObservableProperty] private double totalAssetsCostBase;
   // [ObservableProperty] private double totalAssetsPnLPerc;

   // [ObservableProperty] private double inFlow;
   // [ObservableProperty] private double outFlow;

    [ObservableProperty] private string sortGroup;

    private AssetTotals? selectedAsset = null;
    private AssetAccount? selectedAccount = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowTransactionDialogToAddCommand))]
    private bool isExtendedView = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
    private bool isHidingZeroBalances;
    [ObservableProperty] private bool isHidingCapitalFlow;

    public static List<CoinList>? coinListGecko;
    private bool disposedValue;

    private SortingOrder currentSortingOrder;
    private Func<AssetTotals, object> currentSortFunc;

    public AssetsViewModel(IGraphService graphService, 
            IAssetService assetService, 
            IAccountService accountService,
            ITransactionService transactionService, 
            IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
        Current = this;
        _assetService = assetService;
        _transactionService = transactionService;
        _graphService = graphService;
        _preferencesService = preferencesService;
        _accountService = accountService;

        SortGroup = "Assets";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;

        IsHidingZeroBalances = _preferencesService.GetHidingZeroBalances();
        _assetService.IsHidingZeroBalances = IsHidingZeroBalances;

    }

    /// <summary>
    /// Initialize async task is called from the View_Loading event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    public async Task Initialize()
    {
        

        await _assetService.PopulateAssetTotalsList(currentSortingOrder, currentSortFunc);

        //below setting(s) might have been changed while was moved away from the associated view
        IsHidingCapitalFlow = _preferencesService.GetHidingCapitalFlow();

        //Numberformat might have changed!? So update below numbers to enforce immediate reflection of numberformat change
        _assetService.GetTotalsAssetsValue();
        _assetService.GetTotalsAssetsCostBase();
        _assetService.GetTotalsAssetsPnLPerc();

        await _assetService.GetInFlow();
        await _assetService.GetOutFlow();
    }
    /// <summary>
    /// ReleaseDataSource async task is called from the View_UnLoaded event of the associated View
    /// </summary>
    public void Terminate()
    {
        selectedAccount = null;
        selectedAsset = null;
        IsExtendedView = false;
        _assetService.ClearAssetTotalsList();
    }

    [RelayCommand]
    public void SortOnName(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.Coin.Name;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOn24Hour(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.Coin.Change24Hr;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOn1Month(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.Coin.Change1Month;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOnMarketValue(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.MarketValue;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOnCostBase(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.CostBase;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOnPnL(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.ProfitLoss;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOnPnLPerc(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.ProfitLossPerc;
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
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
            var dialog = new TransactionDialog(_transactionService, _preferencesService, DialogAction.Add)
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
                    .Match(Succ: async newAsset =>
                    {
                        await _assetService.UpdateListAssetTotals(dialog.transactionNew);
                        //check if new transaction is anti-dated
                        if (dialog.transactionNew.TimeStamp.Date < DateTime.Now.Date)
                        {
                            await _graphService.RegisterModification(dialog.transactionNew, null);
                        }
                    },
                        Fail: async err =>
                        {
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_TransactionAddFailed_Title"),
                            err.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Adding Transaction failed");
                        });
                await _assetService.CalculateAssetsTotalValues();
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
            accountAffected = _accountService.GetAffectedAccount(transaction);  //  ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
#pragma warning restore CS8604 // Possible null reference argument.

            var dialog = new TransactionDialog(_transactionService, _preferencesService, DialogAction.Edit, transaction)
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
                            //await UpdateListAssetTotals(dialog.transactionNew);
                            await _assetService.UpdateListAssetTotals(dialog.transactionNew);
                            //await UpdateListAssetAccount(accountAffected);
                            await _accountService.UpdateListAssetAccount(accountAffected);
                            //await UpdateListAssetTransaction(dialog.transactionNew, transaction);
                            await _transactionService.UpdateListAssetTransactions(dialog.transactionNew, transaction);

                            await _graphService.RegisterModification(dialog.transactionNew, transaction);
                        },
                            Fail: async err =>
                            {
                                await ShowMessageDialog(
                                loc.GetLocalizedString("Messages_TransactionUpdateFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                                Logger.Error(err, "Editing Transaction failed");
                            });
                await _assetService.CalculateAssetsTotalValues();
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
                var accountAffected = _accountService.GetAffectedAccount(transaction);
#pragma warning restore CS8604 // Possible null reference argument.

                await (await _transactionService.DeleteTransaction(transaction, accountAffected))
                         .Match(Succ: async s =>
                         {
                             await _assetService.UpdateListAssetTotals(transaction);
                             await _accountService.UpdateListAssetAccount(accountAffected);
                             await _transactionService.RemoveFromListAssetTransactions(transaction);
                             await _graphService.RegisterModification(transaction);
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
        await _transactionService.PopulateTransactionsByAssetList(clickedAccount.AssetId);
    }

    public async Task ShowAccountsAndTransactions(AssetTotals? clickedAsset = null)
    {
       
        if (clickedAsset != null)
        {
            var list = await _accountService.PopulateAccountsByAssetList(clickedAsset.Coin.Id);
            if (list != null && list.Any()) 
            {
                var firstAccount = list.First();
                if (firstAccount is not null)
                {
                    await _transactionService.PopulateTransactionsByAssetList(firstAccount.AssetId);
                }
            }
        }
        else
        {
            _accountService.ClearAccountsByAssetList();
            _transactionService.ClearAssetTransactionsList();
        }
    }

    [RelayCommand]
    public void HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;
    }

}

