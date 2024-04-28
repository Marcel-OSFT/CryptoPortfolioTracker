
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

namespace CryptoPortfolioTracker.ViewModels
{

    public sealed partial class AccountsViewModel : BaseViewModel
    {
        #region Fields related to the MVVM design pattern
        public static AccountsViewModel Current;
        #endregion Fields related to the MVVM design pattern

        #region instances related to Services
        public readonly IAccountService _accountService;
        #endregion instances related to Services

        #region Fields and Proporties for DataBinding with the View

        [ObservableProperty] ObservableCollection<Account> listAccounts;
        [ObservableProperty] ObservableCollection<AssetTotals> listAssetTotals;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
        bool isHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;

        private Account selectedAccount = null;

        #endregion variables and proporties for DataBinding with the View


        public AccountsViewModel(IAccountService accountService)
        {
            //Logger = Log.Logger.ForContext<AccountsViewModel>();
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
            Current = this;
            _accountService = accountService;
            //SetDataSource();

        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowAccountDialogToAddCommand))]
        private bool isExtendedView = false;

        #region MAIN methods or Tasks

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
            ILocalizer loc = Localizer.Get();
            try
            {
                Logger.Information("Showing AccountDialog for Adding");
                AccountDialog dialog = new AccountDialog(Current, DialogAction.Add);
                dialog.XamlRoot = AccountsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    Logger.Information("Adding Account ({0})", dialog.newAccount.Name);
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
            //Account accountToEdit = null;
            ILocalizer loc = Localizer.Get();
            try
            {
                Logger.Information("Showing Account Dialog for Editing");
                //accountToEdit = ListAccounts.Where(t => t.Id == accountId).Single();
                AccountDialog dialog = new AccountDialog(Current, DialogAction.Edit, account);
                dialog.XamlRoot = AccountsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
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
            ILocalizer loc = Localizer.Get();
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
                    ListAssetTotals.Clear();
                    IsExtendedView = false;
                }
            }
            selectedAccount = clickedAccount;
        }

        [RelayCommand]
        public void HideZeroBalances(bool param)
        {
            if (ListAssetTotals == null) return;
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
        public void AssetItemClicked(AssetTotals clickedAsset)
        {
            //In the AccountsView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.

        }


        public async Task ShowAssets(Account clickedAccount)
        {
            (await _accountService.GetAssetsByAccount(clickedAccount.Id))
                .IfSucc(list => ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue)));
            HideZeroBalances(IsHidingZeroBalances);
        }
        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks


        public Task RemoveFromListAccounts(int accountId)
        {
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
        public Task AddToListAccounts(Account newAccount)
        {
            try
            {
                ListAccounts.Add(newAccount);
            }
            catch (Exception) { Task.FromResult(false); }

            return Task.FromResult(true);
        }

        #endregion SUB methods or Tasks


    }

}

