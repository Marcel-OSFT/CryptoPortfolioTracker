
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class AccountsViewModel : BaseViewModel
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AccountsViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public readonly IAccountService _accountService;

    [ObservableProperty] private ObservableCollection<Account>? listAccounts;
    [ObservableProperty] private ObservableCollection<AssetTotals>? listAssetTotals;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
    private bool isHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;

    private Account? selectedAccount = null;

    public AccountsViewModel(IAccountService accountService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
        Current = this;
        _accountService = accountService;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAccountDialogToAddCommand))]
    private bool isExtendedView = false;

    /// <summary>
    /// SetDataSource async task is called from the View_Loading event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task SetDataSource()
    {
        (await _accountService.GetAccounts())
            .IfSucc(list => ListAccounts = new ObservableCollection<Account>(list));
    }

    [RelayCommand(CanExecute = nameof(CanShowAccountDialogToAdd))]
    public async Task ShowAccountDialogToAdd()
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing AccountDialog for Adding");
            var dialog = new AccountDialog(Current, DialogAction.Add)
            {
                XamlRoot = AccountsView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var accountName = dialog.newAccount is not null ? dialog.newAccount.Name : string.Empty;
                Logger.Information("Adding Account ({0})", accountName);
                await (await _accountService.CreateAccount(dialog.newAccount))
                    .Match(Succ: succ => AddToListAccounts(dialog.newAccount),
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
            var dialog = new AccountDialog(Current, DialogAction.Edit, account)
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
                    .Match(Succ: s => RemoveFromListAccounts(account.Id),
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
        if (ListAccounts is null) { return false; }
        var result = false;
        try
        {
            result = !ListAccounts.Where(x => x.Id == account.Id).Single().IsHoldingAsset;
        }
        catch (Exception)
        {
            //Element just removed from the list...
        }
        return result;
    }

    [RelayCommand]
    public async Task AccountItemClicked(Account clickedAccount)
    {
        //Clicked a new Account.... -> Resize Account ListView to small and Show Assets for this Account
        if (selectedAccount == null || selectedAccount != clickedAccount)
        {
            await ShowAssets(clickedAccount);
            IsExtendedView = true;
        }
        //clicked the already selected Account.... ->
        if (selectedAccount != null && selectedAccount == clickedAccount)
        {
            // if Assets are not shown -> Decrease Account Listview to small and show assets for this account
            if (!IsExtendedView)
            {
                await ShowAssets(clickedAccount);
                IsExtendedView = true;
            }
            else //if Assets are shown -> close Assets List and resize Accounts Listview to full-size
            {
                ListAssetTotals?.Clear();
                IsExtendedView = false;
            }
        }
        selectedAccount = clickedAccount;
    }

    [RelayCommand]
    public void HideZeroBalances(bool param)
    {
        if (ListAssetTotals == null)
        {
            return;
        }
        //IsHideZeroBalances = param;
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

    [RelayCommand]
    public static void AssetItemClicked(AssetTotals clickedAsset)
    {
        //In the AccountsView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.
    }

    public async Task ShowAssets(Account clickedAccount)
    {
        (await _accountService.GetAssetsByAccount(clickedAccount.Id))
            .IfSucc(list => ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue)));
        HideZeroBalances(IsHidingZeroBalances);
    }

    public Task RemoveFromListAccounts(int accountId)
    {
        if (ListAccounts is null) { return Task.FromResult(false); }
        try
        {
            var account = ListAccounts.Where(x => x.Id == accountId).Single();
            ListAccounts.Remove(account);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public Task AddToListAccounts(Account? newAccount)
    {
        if (ListAccounts is null || newAccount is null) { return Task.FromResult(false); }
        try
        {
            ListAccounts.Add(newAccount);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }

    

}

