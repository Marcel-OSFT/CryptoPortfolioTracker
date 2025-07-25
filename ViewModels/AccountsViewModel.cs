﻿
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
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
    [ObservableProperty] public string portfolioName = string.Empty;
    [ObservableProperty] public Portfolio currentPortfolio;

    partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    {
        PortfolioName = newValue.Name;
    }


    private readonly IPreferencesService _preferencesService;
    private SortingOrder currentSortingOrder;
    private Func<AssetTotals, object> currentSortFunc;


    [ObservableProperty] private string sortGroup;
    
    //[ObservableProperty]
    //[NotifyCanExecuteChangedFor(nameof(HideZeroBalancesCommand))]
    //private bool isHidingZeroBalances;

    public Account? selectedAccount = null;

    [ObservableProperty] private string glyphPrivacy = "\uE890";

    [ObservableProperty] private bool isPrivacyMode;

    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";

        _preferencesService.SetAreValuesMasked(value);

        _accountService.ReloadValues();
        _assetService.ReloadValues();

    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAccountDialogToAddCommand))]
    public partial bool IsExtendedView { get; set; } = false;

    [ObservableProperty] public partial bool IsAssetsExtendedView { get; set; } = false;

    public AccountsViewModel(IAccountService accountService, IAssetService assetService, IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
        Current = this;
        _accountService = accountService;
        _preferencesService =  preferencesService;
        _assetService = assetService;
        CurrentPortfolio = _assetService.GetPortfolio();

        SortGroup = "Accounts";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;

        _assetService.IsHidingZeroBalances = _preferencesService.GetHidingZeroBalances();
        IsPrivacyMode = _preferencesService.GetAreValesMasked();
    }

    

    /// <summary>
    /// Initialize async task is called from the View_Loaded event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task ViewLoading()
    {
        CurrentPortfolio = _assetService.GetPortfolio();
        PortfolioName = CurrentPortfolio.Name;

        IsPrivacyMode = _preferencesService.GetAreValesMasked();
       // IsHidingZeroBalances = _preferencesService.GetHidingZeroBalances();
        await _accountService.PopulateAccountsList();
    }

    public void Terminate()
    {
        _accountService.ClearAccountsList();
        _accountService.ClearAccountsByAssetList();
        selectedAccount = null;
        IsExtendedView = false;
    }

    
    [RelayCommand]
    public void SortOnName(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.Coin.Name);
    }

    [RelayCommand]
    public void SortOn24Hour(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.Coin.Change24Hr);
    }

    [RelayCommand]
    public void SortOn1Month(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.Coin.Change1Month);
    }

    [RelayCommand]
    public void SortOnMarketValue(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.MarketValue);
    }

    [RelayCommand]
    public void SortOnCostBase(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.CostBase);
    }

    [RelayCommand]
    public void SortOnPnL(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.ProfitLoss);
    }

    [RelayCommand]
    public void SortOnPnLPerc(SortingOrder sortingOrder)
    {
        Sort(sortingOrder, x => x.ProfitLossPerc);
    }

    private void Sort(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    {
        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;
        _assetService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    public static void SortOnNetInvestment(SortingOrder sortingOrder)
    {
        //*** disabled in AccountsView
    }

  
    
    [RelayCommand(CanExecute = nameof(CanShowAccountDialogToAdd))]
    public async Task ShowAccountDialogToAdd()
    {
       
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing AccountDialog for Adding");
            var dialog = new AccountDialog(_preferencesService , Current, DialogAction.Add)
            {
                XamlRoot = AccountsView.Current.XamlRoot
            };
            
            var result = await App.ShowContentDialogAsync(dialog);

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
        
    }
    private bool CanShowAccountDialogToAdd()
    {
        return !IsExtendedView;
    }

    [RelayCommand]
    public async Task ShowAccountDialogToEdit(Account account)
    {
      
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Account Dialog for Editing");
            var dialog = new AccountDialog(_preferencesService , Current, DialogAction.Edit, account)
            {
                XamlRoot = AccountsView.Current.XamlRoot
            };
            var result = await App.ShowContentDialogAsync(dialog);

            if (result == ContentDialogResult.Primary && dialog.newAccount is not null)
            {
                Logger.Information("Editing Account ({0})", account.Name);
                await (await _accountService.EditAccount(dialog.newAccount, account))
                    .Match(Succ: succ => _accountService.UpdateListAccounts(account),
                        Fail: async err =>
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
       
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAccount))]
    public async Task DeleteAccount(Account account)
    {
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
        _assetService.PopulateAssetTotalsByAccountList(selectedAccount, currentSortingOrder, currentSortFunc);

    }

    [RelayCommand]
    public static void AssetItemClicked(AssetTotals clickedAsset)
    {
        //In the AccountsView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.
    }

    public async Task ShowAssets(Account clickedAccount)
    {
        await _assetService.PopulateAssetTotalsByAccountList(clickedAccount, currentSortingOrder, currentSortFunc);
    }

    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }
}

