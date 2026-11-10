
using ABI.Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public sealed partial class AccountsViewModel : BaseViewModel, INotifyPropertyChanged, IRecipient<UpdatePricesMessage>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AccountsViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public IAccountService _accountService {  get; private set; }
    public IAssetService _assetService { get; private set; }

    private readonly IAppState _appState;

    public Settings AppSettings => base._appSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel

    public string PortfolioName => _appState.CurrentPortfolio?.Name ?? string.Empty;
    //[ObservableProperty] public string portfolioName = string.Empty;
    //[ObservableProperty] public Portfolio currentPortfolio;

    //partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    //{
    //    PortfolioName = newValue.Name;
    //}


    private SortingOrder currentSortingOrder;
    private Func<AssetTotalsDisplay, object> currentSortFunc;


    [ObservableProperty] private string sortGroup;
    
    public AccountDisplay? SelectedAccount { get; set; } = null;

    [ObservableProperty] private string glyphPrivacy = "\uE890";

    [ObservableProperty] private bool isPrivacyMode;

    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";

        AppSettings.AreValuesMasked = value;

        //*** force a repaint of the UI to (un)mask values
        MaskRelatedUiRefresh();

        //_accountService.ReloadValues();
        //_assetService.ReloadValues();

    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAccountDialogToAddCommand))]
    public partial bool IsExtendedView { get; set; } = false;

    [ObservableProperty] public partial bool IsAssetsExtendedView { get; set; } = false;
    [ObservableProperty] public partial string ExplainerText { get; set; }
    [ObservableProperty] public partial string ViewTitle { get; set; }
    [ObservableProperty] public partial ObservableCollection<AccountDisplay> ListAccounts { get; set; } = new();
    [ObservableProperty] public partial ObservableCollection<AssetTotalsDisplay>? ListAssetTotals { get; set; } = new ObservableCollection<AssetTotalsDisplay>();
    [ObservableProperty] public partial int AccountsCount { get; set; }
    [ObservableProperty] public partial int? SelectedAccountId { get; set; }
   

    public AccountsViewModel(IAccountService accountService, IAssetService assetService, IAppState appState, Settings appSettings, IDialogService dialogService, IMessenger messenger) : base(appSettings, dialogService, messenger)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AccountsViewModel).Name.PadRight(22));
        Current = this;
        _accountService = accountService;
        _assetService = assetService;
        _appState = appState;
        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PortfolioName));
            // reload data if needed
        };
        messenger.Register<UpdatePricesMessage>(this);

        //CurrentPortfolio = _appState.CurrentPortfolio;

        SortGroup = "Accounts";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;

        _assetService.IsHidingZeroBalances = AppSettings.IsHidingZeroBalances; 
        IsPrivacyMode =AppSettings.AreValuesMasked;
    }

    

    public void Receive(UpdatePricesMessage message)
    {
        _ = RefreshAccountsList();
        _ = RefreshAssetTotalsList();
        AccountsCount = ListAccounts.Count;
    }

    /// <summary>
    /// Initialize async task is called from the View_Loaded event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    /// <returns></returns>
    public async Task ViewLoading()
    {
        ExplainerText = loc.GetLocalizedString("Accounts_Explainer");
        ViewTitle = loc.GetLocalizedString("Accounts_Title");
        //CurrentPortfolio = _appState.CurrentPortfolio;
        //PortfolioName = CurrentPortfolio.Name;

        IsPrivacyMode =AppSettings.AreValuesMasked;
        await PopulateAccountsList();
        
    }

    private void MaskRelatedUiRefresh()
    {
        var tempListAccounts = ListAccounts;
        ListAccounts = null;
        ListAccounts = tempListAccounts;

        var tempListAssetTotals = ListAssetTotals;
        ListAssetTotals = null;
        ListAssetTotals = tempListAssetTotals;
    }

    public void Terminate()
    {
        _accountService.ClearAccountsList();
        _accountService.ClearAccountsByAssetList();
        ListAccounts.Clear();
        SelectedAccount = null;
        IsExtendedView = false;
    }

    
    [RelayCommand]
    public void SortOnName(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Coin.Name);
    }

    [RelayCommand]
    public void SortOn24Hour(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Coin.Change24Hr);
    }

    [RelayCommand]
    public void SortOn1Month(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Coin.Change1Month);
    }

    [RelayCommand]
    public void SortOnMarketValue(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.MarketValue);
    }

    [RelayCommand]
    public void SortOnCostBase(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.CostBase);
    }

    [RelayCommand]
    public void SortOnPnL(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.ProfitLoss);
    }

    [RelayCommand]
    public void SortOnPnLPerc(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.ProfitLossPerc);
    }

    
    [RelayCommand]
    public static void SortOnNetInvestment(SortingOrder sortingOrder)
    {
        //*** disabled in AccountsView
    }

    private void SortList(SortingOrder sortingOrder, Func<AssetTotalsDisplay, object> sortFunc)
    {
        if (ListAssetTotals is null || !ListAssetTotals.Any()) return;

        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;

        if (currentSortingOrder == SortingOrder.Ascending)
        {
            ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(ListAssetTotals.OrderBy(currentSortFunc));
        }
        else
        {
            ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(ListAssetTotals.OrderByDescending(currentSortFunc));
        }

    }


    [RelayCommand(CanExecute = nameof(CanShowAccountDialogToAdd))]
    public async Task ShowAccountDialogToAdd()
    {
       
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing AccountDialog for Adding");

            var newAccount = await _dialogService.ShowAsync<AccountDialog, Account>(new AccountDialogParameters(DialogAction.Add, null));

            if (newAccount != null)
            {
                Logger.Information("Adding Account ({0})", newAccount.Name);
                await (await _accountService.CreateAccount(newAccount))
                    .Match(Succ: async succ => await AddToListAccounts(newAccount),
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
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
            await _dialogService.ShowMessageAsync(
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
    public async Task ShowAccountDialogToEdit(AccountDisplay account)
    {
      
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Account Dialog for Editing");
            var domainAccount = ModelMapper.MapToDomain(account);
            var newAccount = await _dialogService.ShowAsync<AccountDialog, Account>(new AccountDialogParameters(DialogAction.Edit, domainAccount));

            if (newAccount != null)
            {
                Logger.Information("Editing Account ({0})", account.Name);
                await (await _accountService.EditAccount(newAccount, domainAccount))
                    .Match(Succ: async succ => await UpdateListAccounts(domainAccount),
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
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
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_AccountDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
       
    }

    [RelayCommand(CanExecute = nameof(CanDeleteAccount))]
    public async Task DeleteAccount(AccountDisplay account)
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
                    .Match(Succ: s => RemoveFromListAccounts(account.Id),
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
                            loc.GetLocalizedString("Messages_AccountDeleteFailed_Title"),
                            err.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Deleting Account failed");
                        });
            }
            else
            {
                Logger.Information("Deleting Account not allowed");

                await _dialogService.ShowMessageAsync(
                    loc.GetLocalizedString("Messages_AccountDeleteNotAllowd_Title"),
                    loc.GetLocalizedString("Messages_AccountDeleteNotAllowed_Msg"),
                    loc.GetLocalizedString("Common_CloseButton"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to delete Account");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_AccountDeleteFailed_Title"),
                            ex.Message,
                            loc.GetLocalizedString("Common_CloseButton"));
        }
       
    }
    private bool CanDeleteAccount(AccountDisplay account)
    {
        var item = ListAccounts.FirstOrDefault(x => x.Id == account.Id);
        return item != null ? item.IsHoldingAsset ? false : true : false;
    }

    [RelayCommand]
    public async Task AccountItemClicked(AccountDisplay clickedAccount)
    {
        //Clicked a new Account.... -> Resize Account ListView to small and Show Assets for this Account
        if (SelectedAccount == null || SelectedAccount.Id != clickedAccount.Id)
        {
            IsExtendedView = true;
            await ShowAssets(clickedAccount);
            SelectedAccount = clickedAccount;
        }
        //clicked the already selected Account.... ->
        else if (SelectedAccount != null && SelectedAccount.Id == clickedAccount.Id)
        {
            // if Assets are not shown -> Decrease Account Listview to small and show assets for this account
            if (!IsExtendedView)
            {
                IsExtendedView = true;
                await ShowAssets(clickedAccount);
                SelectedAccount = clickedAccount;
            }
            else //if Assets are shown -> close Assets List and resize Accounts Listview to full-size
            {
                //_assetService.ClearAssetTotalsList();
                ListAssetTotals?.Clear();
                IsExtendedView = false;
                SelectedAccount = null;
            }
        }
        
    }

    [RelayCommand]
    public async Task HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;
        //_assetService.PopulateAssetTotalsByNarrativeList(selectedNarrative, currentSortingOrder, currentSortFunc);
        await ShowAssets(SelectedAccount);
    }

    [RelayCommand]
    public static void AssetItemClicked(AssetTotalsDisplay clickedAsset)
    {
        //In the AccountsView we ignore this command. The AssetsListViewControl is used in the AssetsView as well.
    }


    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }



    private async Task PopulateAccountsList()
    {
        var list = (await _accountService.PopulateAccountsList())
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        ListAccounts = new ObservableCollection<AccountDisplay>(list);
        AccountsCount = ListAccounts.Count;
    }


    private async Task UpdateListAccounts(Account account)
    {
        var selectedAccount = SelectedAccountId;
        var UpdatedAccounts = (await _accountService.UpdateListAccounts(account))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        ReplaceOrAddUpdatedItems(ListAccounts, UpdatedAccounts, x => x.Id);
        AccountsCount = ListAccounts.Count;
        if (selectedAccount.HasValue)
        {
            var selectedItem = ListAccounts.FirstOrDefault(x => x.Id == selectedAccount.Value);
            SelectedAccountId = selectedItem?.Id;
        }
    }

    private async Task AddToListAccounts(Account account)
    {
        var result = await _accountService.AddToListAccounts(account);
        if (result)
        {
            ListAccounts.Add(ModelMapper.MapToDisplay(account));
            AccountsCount = ListAccounts.Count;
        }
    }

    private async Task RemoveFromListAccounts(int accountId)
    {
        var result = await _accountService.RemoveFromListAccounts(accountId);
        if (result)
        {
            var accountToRemove = ListAccounts.FirstOrDefault(x => x.Id == accountId);
            if (accountToRemove != null)
            {
                ListAccounts.Remove(accountToRemove);
                AccountsCount = ListAccounts.Count;
            }
        }
    }

    private async Task ShowAssets(AccountDisplay clickedAccount)
    {
        var list = (await _assetService.PopulateAssetTotalsByAccountList(ModelMapper.MapToDomain(clickedAccount)))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? list.OrderBy(currentSortFunc).ToList()
            : list.OrderByDescending(currentSortFunc).ToList();

        ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(sorted);
    }

    private async Task RefreshAssetTotalsList()
    {
        if (ListAssetTotals == null || ListAssetTotals.Count == 0) { return; }

        var selectedAccount = ListAccounts.FirstOrDefault(x => x.Id == SelectedAccountId.Value);

        var refreshedList = (await _assetService.PopulateAssetTotalsByAccountList(ModelMapper.MapToDomain(selectedAccount)))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        foreach (var item in refreshedList)
        {
            // Find the corresponding item in the existing collection and update its properties
            var existingItem = ListAssetTotals?.FirstOrDefault(x => x.Coin.Id == item.Coin.Id);
            if (existingItem != null)
            {
                existingItem.Coin.Price = item.Coin.Price;
                existingItem.Coin.Change1Month = item.Coin.Change1Month;
                existingItem.Coin.Change24Hr = item.Coin.Change24Hr;

                existingItem.MarketValue = item.MarketValue;
                existingItem.ProfitLoss = item.ProfitLoss;
                existingItem.ProfitLossPerc = item.ProfitLossPerc;
                existingItem.NetProfitLossPerc = item.NetProfitLossPerc;
            }
        }
        var copy = new List<AssetTotalsDisplay>(ListAssetTotals);
        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? copy.OrderBy(currentSortFunc).ToList()
            : copy.OrderByDescending(currentSortFunc).ToList();

        CollectionReorderHelper.ApplyOrderedSequence(ListAssetTotals, sorted, x => x.Coin.Id);
    }

    private async Task RefreshAccountsList()
    {
        if (ListAccounts == null || ListAccounts.Count == 0) { return; }
        var refreshedList = await _accountService.PopulateAccountsList();
        foreach (var item in refreshedList)
        {
            // Find the corresponding item in the existing collection and update its properties
            var existingItem = ListAccounts?.FirstOrDefault(x => x.Id == item.Id);
            if (existingItem != null)
            {
                existingItem.TotalValue = item.TotalValue;
            }
        }
    }





}

