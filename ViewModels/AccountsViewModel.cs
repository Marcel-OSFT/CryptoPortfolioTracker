
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Helpers;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;
using System.ComponentModel;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class AccountsViewModel : BaseViewModel, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AccountsViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public IAccountService _accountService {  get; private set; }
    public IAssetService _assetService { get; private set; }

    private readonly IPreferencesService _preferencesService;

    //[ObservableProperty] private ObservableCollection<Account>? listAccounts;
    //[ObservableProperty] private ObservableCollection<AssetTotals>? listAssetTotals;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
    private bool isHidingZeroBalances;

    private Account? selectedAccount = null;

    public AccountsViewModel(IAccountService accountService, IAssetService assetService, IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
        Current = this;
        _accountService = accountService;
        _preferencesService =  preferencesService;
        _assetService = assetService;
        

        IsHidingZeroBalances = _preferencesService.GetHidingZeroBalances();
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAccountDialogToAddCommand))]
    private bool isExtendedView = false;

    /// <summary>
    /// SetDataSource async task is called from the View_Loading event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task Initialize()
    {
        //var getAccountsResult = await _accountService.GetAccounts();
        //getAccountsResult.IfSucc(s => CreateListAccounts(s));

        await _accountService.PopulateAccountsList();
    }

    public void Terminate()
    {
       // ListAssetTotals = MkOsft.NullObservableCollection<AssetTotals>(ListAssetTotals);
       // ListAccounts = MkOsft.NullObservableCollection<Account>(ListAccounts);

        _accountService.ClearAccountsByAssetList();

        selectedAccount = null;
        IsExtendedView = false;
    }

    //public async Task<bool> CreateListAccounts(List<Account> list)
    //{
    //    ListAccounts = new ObservableCollection<Account>(list.OrderByDescending(x => x.TotalValue));

    //    return ListAccounts.Any();
    //}

    //private Task DoSorting(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    //{
    //    if (ListAssetTotals is not null)
    //    {
    //        if (sortingOrder == SortingOrder.Ascending)
    //        {
    //            ListAssetTotals = new ObservableCollection<AssetTotals>(ListAssetTotals.OrderBy(sortFunc));
    //        }
    //        else
    //        {
    //            ListAssetTotals = new ObservableCollection<AssetTotals>(ListAssetTotals.OrderByDescending(sortFunc));
    //        }
    //    }
    //    return Task.CompletedTask;
    //}
    [RelayCommand]
    public void SortOnName(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.Coin.Name;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOn24Hour(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.Coin.Change24Hr;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOn1Month(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.Coin.Change1Month;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public void SortOnMarketValue(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.MarketValue;
        _assetService.SortList(sortingOrder, sortFunc);

    }
    [RelayCommand]
    public void SortOnCostBase(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.CostBase;
        _assetService.SortList(sortingOrder, sortFunc);

    }
    [RelayCommand]
    public void SortOnPnL(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.ProfitLoss;
        _assetService.SortList(sortingOrder, sortFunc);

    }
    [RelayCommand]
    public void SortOnPnLPerc(SortingOrder sortingOrder)
    {
        Func<AssetTotals, object> sortFunc = x => x.ProfitLossPerc;
        _assetService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand(CanExecute = nameof(CanShowAccountDialogToAdd))]
    public async Task ShowAccountDialogToAdd()
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing AccountDialog for Adding");
            var dialog = new AccountDialog(_preferencesService , Current, DialogAction.Add)
            {
                XamlRoot = AccountsView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var accountName = dialog.newAccount is not null ? dialog.newAccount.Name : string.Empty;
                Logger.Information("Adding Account ({0})", accountName);
                await (await _accountService.CreateAccount(dialog.newAccount))
                    .Match(Succ: succ => _accountService.AddToListAccounts(dialog.newAccount),
                        Fail: async err =>
                        {
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_AccountAddFailed_Title"),
                            err.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Adding Account failed");
                        });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing Account Dialog failed");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_AccountDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }
    private bool CanShowAccountDialogToAdd()
    {
        return !IsExtendedView;
    }

    [RelayCommand]
    public async Task ShowAccountDialogToEdit(Account account)
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Account Dialog for Editing");
            var dialog = new AccountDialog(_preferencesService , Current, DialogAction.Edit, account)
            {
                XamlRoot = AccountsView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && dialog.newAccount is not null)
            {
                Logger.Information("Editing Account ({0})", account.Name);
                (await _accountService.EditAccount(dialog.newAccount, account))
                    .IfFail(async err =>
                    {
                        await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_AccountUpdateFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));
                        Logger.Error(err, "Updating Account failed");
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Account Dialog");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_AccountDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAccount))]
    public async Task DeleteAccount(Account account)
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        // *** Delete option normally never available when an Account contains assets
        //*** but nevertheless lets check it...
        try
        {
            Logger.Information("Deletion request for Account ({0})", account.Name);
            var IsDeleteAllowed = (await _accountService.AccountHasNoAssets(account.Id))
            .Match(Succ: succ => succ, Fail: err => { return false; });

            if (IsDeleteAllowed)
            {
                Logger.Information("Deleting Account");
                await (await _accountService.RemoveAccount(account.Id))
                    .Match(Succ: s => _accountService.RemoveFromListAccounts(account.Id),
                        Fail: async err =>
                        {
                            await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_AccountDeleteFailed_Title"),
                            err.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Deleting Account failed");
                        });
            }
            else
            {
                Logger.Information("Deleting Account not allowed");

                await ShowMessageDialog(
                    loc.GetLocalizedString("Messages_AccountDeleteNotAllowd_Title"),
                    loc.GetLocalizedString("Messages_AccountDeleteNotAllowed_Msg"),
                    loc.GetLocalizedString("Common_CloseButton"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete Account");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_AccountDeleteFailed_Title"),
                            ex.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }
    private bool CanDeleteAccount(Account account)
    {
        return !_accountService.IsAccountHoldingAssets(account);   
    }

    [RelayCommand]
    public async Task AccountItemClicked(Account clickedAccount)
    {
        //Clicked a new Account.... -> Resize Account ListView to small and Show Assets for this Account
        if (selectedAccount == null || selectedAccount != clickedAccount)
        {
            IsExtendedView = true;
            await ShowAssets(clickedAccount);
        }
        //clicked the already selected Account.... ->
        if (selectedAccount != null && selectedAccount == clickedAccount)
        {
            // if Assets are not shown -> Decrease Account Listview to small and show assets for this account
            if (!IsExtendedView)
            {
                IsExtendedView = true;
                await ShowAssets(clickedAccount);
            }
            else //if Assets are shown -> close Assets List and resize Accounts Listview to full-size
            {
                _assetService.ClearAssetTotalsList();
                IsExtendedView = false;
            }
        }
        selectedAccount = clickedAccount;
    }

    [RelayCommand]
    public void HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;

    //    //if (ListAssetTotals == null)
    //    //{
    //    //    return;
    //    //}
    //    ////IsHideZeroBalances = param;
    //    //if (param)
    //    //{
    //    //    var itemsToHide = ListAssetTotals.Where(x => x.MarketValue <= 0).ToList();
    //    //    foreach (var item in itemsToHide)
    //    //    {
    //    //        item.IsHidden = true; ;
    //    //    }
    //    //}
    //    //else
    //    //{
    //    //    foreach (var item in ListAssetTotals)
    //    //    {
    //    //        item.IsHidden = false; ;
    //    //    }
    //    //}
    }

    [RelayCommand]
    public static void AssetItemClicked(AssetTotals clickedAsset)
    {
        //In the AccountsView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.
    }

    public async Task ShowAssets(Account clickedAccount)
    {

        await _assetService.PopulateAssetTotalsByAccountList(clickedAccount);

        //(await _accountService.GetAssetsByAccount(clickedAccount.Id))
        //    .IfSucc(list => ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue)));

        //_assetService.IsHidingZeroBalances = IsHidingZeroBalances;
    }

    //public Task RemoveFromListAccounts(int accountId)
    //{
    //    if (ListAccounts is null) { return Task.FromResult(false); }
    //    try
    //    {
    //        var account = ListAccounts.Where(x => x.Id == accountId).Single();
    //        ListAccounts.Remove(account);
    //    }
    //    catch (Exception)
    //    {
    //        return Task.FromResult(false);
    //    }
    //    return Task.FromResult(true);
    //}

    //public Task AddToListAccounts(Account? newAccount)
    //{
    //    if (ListAccounts is null || newAccount is null) { return Task.FromResult(false); }
    //    try
    //    {
    //        ListAccounts.Add(newAccount);
    //    }
    //    catch (Exception) { Task.FromResult(false); }

    //    return Task.FromResult(true);
    //}

}

