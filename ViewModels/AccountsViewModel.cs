﻿
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using System.Windows.Input;
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
            Current = this;
            _accountService = accountService;
            SetDataSource();
           
        }
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ShowAccountDialogToAddCommand))]
        private bool isExtendedView = false;

        #region MAIN methods or Tasks
        private async Task SetDataSource()
        {
            (await _accountService.GetAccounts())
                .IfSucc(list => ListAccounts = new ObservableCollection<Account>(list));
        }
        
        [RelayCommand(CanExecute = nameof(CanShowAccountDialogToAdd))]
        public async Task ShowAccountDialogToAdd()
        {
            try
            {
                AccountDialog dialog = new AccountDialog(Current, DialogAction.Add);
                dialog.XamlRoot = AccountsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    await (await _accountService.CreateAccount(dialog.newAccount))
                        .Match(Succ: succ => AddToListAccounts(dialog.newAccount),
                            Fail: async err => await ShowMessageBox("Adding account failed", err.Message));
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Account Dialog Failure", ex.Message);
            }
        }
        private bool CanShowAccountDialogToAdd()
        {
            return !IsExtendedView;
        }

        [RelayCommand]
        public async Task ShowAccountDialogToEdit(int accountId)
        {
            Account accountToEdit = null;
            try
            {
                accountToEdit = ListAccounts.Where(t => t.Id == accountId).Single();
                AccountDialog dialog = new AccountDialog(Current, DialogAction.Edit);
                dialog.XamlRoot = AccountsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    
                    (await _accountService.EditAccount(dialog.newAccount, accountToEdit))
                        .IfFail(async err => await ShowMessageBox("Updating account failed", err.Message));
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Account Dialog Failure", ex.Message);
            }
        }

        [RelayCommand(CanExecute = nameof(CanDeleteAccount))]
        public async Task DeleteAccount(int accountId)
        {
            // *** Delete option normally never available when an Account contains assets
            //*** but nevertheless lets check it...

            bool IsDeleteAllowed = (await _accountService.AccountHasNoAssets(accountId))
                .Match<bool>(Succ: succ => succ, Fail: err => { return false; });

            if (IsDeleteAllowed)
            {
                await (await _accountService.RemoveAccount(accountId))
                    .Match(Succ: s => RemoveFromListAccounts(accountId),
                        Fail: async err => await ShowMessageBox("Failed to delete Account", err.Message));
            }
            else
            {
                await ShowMessageBox("Account deletion failed", "Account may have assets assigned.");
            }

        }
        private bool CanDeleteAccount(int accountId)
        {
            bool result=false;
            try
            {
                result = !ListAccounts.Where(x => x.Id == accountId).Single().IsHoldingAsset;
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
                    IsExtendedView= true;
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



        public async Task ShowAssets(Account clickedAccount)
        {
            (await _accountService.GetAssetsByAccount(clickedAccount.Id))
                .IfSucc(list => ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue)));
            HideZeroBalances(IsHidingZeroBalances);
        }
        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks

        
        public Task<bool> RemoveFromListAccounts(int accountId)
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
        public Task<bool> AddToListAccounts(Account newAccount)
        {
            try
            {
                ListAccounts.Add(newAccount);
            }
            catch (Exception)
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        private async Task ShowMessageBox(string title, string message)
        {
            ContentDialog errorDialog = new ContentDialog()
            {
                Title = title,
                XamlRoot = AccountsView.Current.XamlRoot,
                Content = message,
                PrimaryButtonText = "OK"
            };
            await errorDialog.ShowAsync();
        }
        #endregion SUB methods or Tasks


    }

}

