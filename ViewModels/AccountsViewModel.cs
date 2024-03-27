
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
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Storage.Provider;
using Windows.UI.Popups;
using LanguageExt.Common;

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
        private ObservableCollection<Account> listAccounts;
        public ObservableCollection<Account> ListAccounts
        {
            get { return listAccounts; }
            set
            {
                if (listAccounts == value) return;
                listAccounts = value;
                OnPropertyChanged(nameof(ListAccounts));
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
        #endregion variables and proporties for DataBinding with the View

        public AccountsViewModel(IAccountService accountService)
        {
            Current = this;
            _accountService = accountService;
            SetDataSource();
        }

        #region MAIN methods or Tasks
        private async Task SetDataSource()
        {
            (await _accountService.GetAccounts())
                .IfSucc(list => ListAccounts = new ObservableCollection<Account>(list));
        }
        public async Task ShowAccountDialog(DialogAction dialogAction, int accountId = 0)
        {
            Account accountToEdit = null;
            try
            {
                if (dialogAction == DialogAction.Edit)
                {
                    accountToEdit = ListAccounts.Where(t => t.Id == accountId).Single();
                }
                AccountDialog dialog = new AccountDialog(Current, dialogAction, accountToEdit);
                dialog.XamlRoot = AccountsView.Current.XamlRoot;
                var result = await dialog.ShowAsync();
            
                if (result == ContentDialogResult.Primary)
                {
                    if (dialogAction == DialogAction.Add)
                    {
                        await (await _accountService.CreateAccount(dialog.newAccount))
                         .Match(Succ: succ => AddToListAccounts(dialog.newAccount),
                             Fail: async err => await ShowMessageBox("Adding account failed - " + err.Message));
                    }
                    else
                    {
                         (await _accountService.EditAccount(dialog.newAccount, accountToEdit))
                            .IfFail( async err => await ShowMessageBox("Updating account failed - " + err.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageBox("Failure on showing Account Dialog (" + ex.Message + ")");
            }
        }
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
                        Fail: async err => await ShowMessageBox("Failed to delete Account"));
            }
            else
            {
                await ShowMessageBox("Account can't be deleted. It might have assets assigned.");
            }

        }
        public async Task ShowAssets(Account clickedAccount)
        {
            (await _accountService.GetAssetsByAccount(clickedAccount.Id))
                .IfSucc(list => ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue)));
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
        private async Task ShowMessageBox(string message, string primaryButtonText = "OK", string closeButtonText = "Close")
        {
            var dlg = new MsgBoxDialog(message);
            dlg.XamlRoot = AccountsView.Current.XamlRoot;
            dlg.PrimaryButtonText = primaryButtonText;
            dlg.CloseButtonText = closeButtonText;
            await dlg.ShowAsync();
        }
        #endregion SUB methods or Tasks


    }

}

