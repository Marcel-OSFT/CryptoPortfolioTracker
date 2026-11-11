
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.UI;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

[ObservableRecipient]
public sealed partial class AssetsViewModel : BaseViewModel, IRecipient<UpdatePricesMessage>, IRecipient<PortfolioConnectionChangedMessage>, IRecipient<CurrencyChangedMessage>
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AssetsViewModel Current;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public IAssetService _assetService {  get; private set; }
    public ITransactionService _transactionService { get; private set; }
    public IAccountService _accountService { get; private set; }
    //private readonly IDialogService _dialogService;

    private readonly ICapitalFlowService _capitalFlowService;
    private readonly IAppState _appState;

    public double InFlowForCurrency => _capitalFlowService.GetInFlowForCurrency();
    public double OutFlowForCurrency => _capitalFlowService.GetOutFlowForCurrency();
    public string PortfolioName => _appState.CurrentPortfolio?.Name ?? string.Empty;
    public Settings AppSettings  => base._appSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel
    
    private readonly IGraphService _graphService;

    [ObservableProperty] public partial string SortGroup { get; set; }

    public AssetTotalsDisplay? SelectedAsset { get; set; } = null;
    private AssetAccount? selectedAccount = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowTransactionDialogToAddCommand))]
    public partial bool IsAssetsExtendedView { get; set; } = false;

    public static List<CoinList>? coinListGecko;
    private bool disposedValue;

    private SortingOrder currentSortingOrder;
    private Func<AssetTotalsDisplay, object> currentSortFunc;

    [ObservableProperty] public partial string GlyphPrivacy { get; set; } = "\uE890";

    [ObservableProperty] public partial bool IsPrivacyMode { get; set; }

    [ObservableProperty] public partial string ExplainerText { get; set; }
    [ObservableProperty] public partial string ViewTitle { get; set; }

    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";
        AppSettings.AreValuesMasked = value;
        MaskRelatedUiRefresh();
        ReloadTotals();
    }

    [ObservableProperty] public partial double TotalPortfolioValue { get; set; }
    [ObservableProperty] public partial double TotalCostBase { get; set; }
    [ObservableProperty] public partial double TotalPnlPerc { get; set; }
    [ObservableProperty] public partial long VisibleAssetsCount { get; set; }


    [ObservableProperty] public partial CapitalFlowData CapitalFlow { get; set; }
    [ObservableProperty] public partial ObservableCollection<AssetAccount>? ListAssetAccounts { get; set; } = new ObservableCollection<AssetAccount>();
    [ObservableProperty] public partial ObservableCollection<AssetTotalsDisplay>? ListAssetTotals { get; set; } = new ObservableCollection<AssetTotalsDisplay>();
    [ObservableProperty] public partial ObservableCollection<TransactionDisplay>? ListAssetTransactions { get; set; } = new ObservableCollection<TransactionDisplay>();
    [ObservableProperty] public partial int? SelectedAccountId { get; set; }
    [ObservableProperty] public partial string? SelectedAssetApiId { get; set; } 

    public bool IsHidingZeroBalances => _assetService.IsHidingZeroBalances;
    

    partial void OnCapitalFlowChanged(CapitalFlowData value)
    {
        _capitalFlowService.GetNetFlowForCurrency();
    }

    public AssetsViewModel(IGraphService graphService, 
                            IAssetService assetService, 
                            IAccountService accountService, 
                            IAppState appState,
                            ITransactionService transactionService, 
                            IMessenger messenger, 
                            ICapitalFlowService capitalFlowService,
                            IDialogService dialogService,
                            Settings settings) : base (settings, dialogService, messenger)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
        Current = this;

        messenger.Register<UpdatePricesMessage>(this);
        messenger.Register<PortfolioConnectionChangedMessage>(this);
        messenger.Register<CurrencyChangedMessage>(this);
        
        _assetService = assetService;
        _transactionService = transactionService;
        _graphService = graphService;
        _accountService = accountService;
        _capitalFlowService = capitalFlowService;
        _appState = appState;
        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PortfolioName));
            // reload data if needed
        };

        InitializeView();

    }

    public void Receive(UpdatePricesMessage message)
    {
        if (message is null) return;
        _ = RefreshDataAsync();
    }

    public void Receive(PortfolioConnectionChangedMessage message)
    {
        if (message is null) return;
        OnPropertyChanged(nameof(PortfolioName)); // Refreshes portfolioName in case Portfolio has switched.
    
    }

    public void Receive(CurrencyChangedMessage message)
    {
        if (message is null) return;
        OnPropertyChanged(nameof(InFlowForCurrency));
        OnPropertyChanged(nameof(OutFlowForCurrency));
    }

    /// <summary>
    /// Initialize async task is called from the View_Loaded event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    private async Task InitializeView()
    {
        

        SortGroup = "Assets";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;
        _assetService.IsHidingZeroBalances = AppSettings.IsHidingZeroBalances;
        CapitalFlow = _capitalFlowService.CapitalFlow;
        IsPrivacyMode = AppSettings.AreValuesMasked;
        ExplainerText = loc.GetLocalizedString("Assets_Explainer");
        ViewTitle = loc.GetLocalizedString("Assets_Title");
    }

    
    public async Task ViewLoading()
    {
        await LoadViewDataAsync();
    }

    private async Task LoadViewDataAsync()
    {
        //OnPropertyChanged(nameof(PortfolioName)); // gets portfolioname again in case Portfolio has switched.

        await PopulateAssetTotalsList();

        //below setting(s) might have been changed while was moved away from the associated view

        //Numberformat might have changed!? So update below numbers to enforce immediate reflection of numberformat change
        await GetPortfolioTotals();
    }
    private async Task RefreshDataAsync()
    {
        await RefreshAssetTotalsList(currentSortingOrder, currentSortFunc);
        await GetPortfolioTotals();
    }

    private async Task GetPortfolioTotals()
    {
        TotalPortfolioValue = 0;
        TotalCostBase = 0;
        TotalPnlPerc = 0;
        TotalPortfolioValue = _assetService.GetTotalsAssetsValue();
        TotalCostBase = _assetService.GetTotalsAssetsCostBase();
        TotalPnlPerc = _assetService.GetTotalsAssetsPnLPerc();
        VisibleAssetsCount = _assetService.GetVisibleAssetsCount();
        await _assetService.CalculateYearlyCapitalFlow();
    }


    /// <summary>
    /// ReleaseDataSource async task is called from the View_UnLoaded event of the associated View
    /// </summary>
    public void Terminate()
    {
        selectedAccount = null;
        SelectedAsset = null;
        IsAssetsExtendedView = false;
        
    }

    private void ReloadTotals()
    {
        OnPropertyChanged(nameof(TotalPortfolioValue));
        OnPropertyChanged(nameof(TotalCostBase));
        OnPropertyChanged(nameof(InFlowForCurrency));
        OnPropertyChanged(nameof(OutFlowForCurrency));

    }

    private void MaskRelatedUiRefresh()
    {
        var temp0 = ListAssetAccounts;
        ListAssetAccounts = null;
        ListAssetAccounts = temp0;

        var temp1 = ListAssetTransactions;
        ListAssetTransactions = null;
        ListAssetTransactions = temp1;

        var temp2 = ListAssetTotals;
        ListAssetTotals = null;
        ListAssetTotals = temp2;
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
    public void SortOnNetInvestment(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.NetInvestment);
        
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
    public void SortOnNetPnLPerc(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.NetProfitLossPerc);
        
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


    [RelayCommand]
    public async Task AssetItemClicked(AssetTotalsDisplay clickedAsset)
    {
        //new item clicked and selected ?
        if (SelectedAsset == null || SelectedAsset.Coin.Id != clickedAsset.Coin.Id)
        {
            await ShowAccountsAndTransactions(clickedAsset);
            SelectedAsset = clickedAsset;
            IsAssetsExtendedView = true;
        }
        //clicked already selected item
        else if (SelectedAsset != null && SelectedAsset.Coin.Id == clickedAsset.Coin.Id)
        {
            if (!IsAssetsExtendedView)
            {
                await ShowAccountsAndTransactions(clickedAsset);
                IsAssetsExtendedView = true;
                SelectedAsset = clickedAsset;
            }
            else
            {
                ClearAccountsAndTransactions();
                SelectedAsset = null;
                IsAssetsExtendedView = false;
            }
        }
    }


    public async Task ShowAccountsAndTransactions(AssetTotalsDisplay? clickedAsset = null)
    {
        if (clickedAsset == null) return;
        
        var result = await PopulateAccountsByAssetList(clickedAsset.Coin.Id);
        if (result != null && result.Any())
        {
            var firstAccount = result.First();
            if (firstAccount is not null)
            {
                await PopulateTransactionsByAssetList(firstAccount.AssetId);
            }
        }
    }


    [RelayCommand]
    public async Task AccountItemClicked(AssetAccount clickedAccount)
    {
        //new item clicked and selected?
        if (selectedAccount == null || selectedAccount.AssetId != clickedAccount.AssetId)
        {
            await PopulateTransactionsByAssetList(clickedAccount.AssetId);
        }
        selectedAccount = clickedAccount;
    }

    

    [RelayCommand(CanExecute = nameof(CanShowTransactionDialogToAdd))]
    public async Task ShowTransactionDialogToAdd()
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Adding");

            var transaction = await _dialogService.ShowAsync<TransactionDialog, Transaction>(new TransactionDialogParameters(DialogAction.Add, null));

            if (transaction != null)
            {
                Logger.Information("Adding a new Transaction - {0}", transaction.Details.TransactionType);
                await (await _transactionService.AddTransaction(transaction))
                    .Match(Succ: async newAsset =>
                    {
                        await UpdateListAssetTotals(transaction);
                        //check if new transaction is anti-dated
                        if (transaction.TimeStamp.Date < DateTime.Now.Date)
                        {
                            await _graphService.RegisterModification(transaction, null);
                        }
                    },
                        Fail: async err =>
                        {
                            await _dialogService.ShowMessageAsync(
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
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_TransactionDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        
    }
    private bool CanShowTransactionDialogToAdd()
    {
        return !IsAssetsExtendedView;
    }

    [RelayCommand]
    public async Task ShowTransactionDialogToEdit(TransactionDisplay transactionToEdit)
    {
        AssetAccount? accountAffected = null;
        var domainTransactionToEdit = ModelMapper.MapToDomain(transactionToEdit);
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Editing");
            //*** editing a transaction also involves a change for an element in the ListAssetAccounts
            accountAffected = ListAssetAccounts.Where(t => t.AssetId == transactionToEdit.RequestedAsset.Id).Single();

            var modifiedTransaction = await _dialogService.ShowAsync<TransactionDialog, Transaction>(new TransactionDialogParameters(DialogAction.Edit, domainTransactionToEdit));

            if (modifiedTransaction != null)
            {
                Logger.Information("Editing Transaction ({0}) - {1}", transactionToEdit.Id, transactionToEdit.Details.TransactionType);
                await (await _transactionService.EditTransaction(modifiedTransaction, domainTransactionToEdit))
                        .Match(Succ: async newAsset =>
                        {
                            await UpdateListAssetTotals(modifiedTransaction);
                            await UpdateListAssetAccount(accountAffected);
                            await UpdateListAssetTransactions(modifiedTransaction);

                            await _graphService.RegisterModification(modifiedTransaction, domainTransactionToEdit);
                        },
                            Fail: async err =>
                            {
                                await _dialogService.ShowMessageAsync(
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

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_TransactionDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    [RelayCommand]
    public async Task DeleteTransaction(TransactionDisplay transaction)
    {
        var loc = Localizer.Get();
        var domainTransaction = ModelMapper.MapToDomain(transaction);
        try
        {
            Logger.Information("Deletion Request for Transaction ({0}) - {1}", transaction.Id, transaction.Details.TransactionType);

            var dlgResult = await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_TransactionDelete_Title"),
                loc.GetLocalizedString("Messages_TransactionDelete_Msg"),
                loc.GetLocalizedString("Common_ConfirmButton"),
                loc.GetLocalizedString("Common_CancelButton"));

            if (dlgResult == ContentDialogResult.Primary)
            {
                Logger.Information("Deleting Transaction");
                //*** editing a transaction also involves a change for an element in the ListAssetAccounts
                var accountAffected = _accountService.GetAffectedAccount(domainTransaction);

                await (await _transactionService.DeleteTransaction(domainTransaction, accountAffected))
                         .Match(Succ: async s =>
                         {
                             await UpdateListAssetTotals(domainTransaction);
                             await _accountService.UpdateListAssetAccount(accountAffected);
                             await _transactionService.RemoveFromListAssetTransactions(domainTransaction);
                             await _graphService.RegisterModification(domainTransaction);
                         },
                            Fail: async err =>
                            {
                                await _dialogService.ShowMessageAsync(
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

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_TransactionDeleteFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    
    

    [RelayCommand]
    public async Task HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;
        await PopulateAssetTotalsList();
        VisibleAssetsCount = _assetService.GetVisibleAssetsCount();
        OnPropertyChanged(nameof(IsHidingZeroBalances));
    }

    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }

    [RelayCommand]
    public async Task ShowSettingsDialog()
    {
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing PortfolioSettings Dialog");
            await _dialogService.ShowAsync<PortfolioSettingsDialog, bool>();

        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show PortfolioSettings Dialog");
            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_PortfolioSettingsDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    

    private async Task PopulateAssetTotalsList()
    {
        var list = (await _assetService.PopulateAssetTotalsList())
                .Select(ModelMapper.MapToDisplay)
                .ToList();
       
        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? list.OrderBy(currentSortFunc).ToList()
            : list.OrderByDescending(currentSortFunc).ToList();

        ListAssetTotals = new ObservableCollection<AssetTotalsDisplay>(sorted);
    }

    private async Task<ObservableCollection<AssetAccount>> PopulateAccountsByAssetList(int coinId)
    {
        var list = (await _accountService.PopulateAccountsByAssetList(coinId)).ToList();

        ListAssetAccounts = new ObservableCollection<AssetAccount>(list);

        return ListAssetAccounts;
    }

    public async Task PopulateTransactionsByAssetList(int accountId)
    {
        var list = (await _transactionService.PopulateTransactionsByAssetList(accountId))
                .Select(ModelMapper.MapToDisplay)
                .ToList();

        ListAssetTransactions = new ObservableCollection<TransactionDisplay>(list);
    }


    private async Task RefreshAssetTotalsList(SortingOrder currentSortingOrder, Func<AssetTotalsDisplay, object> currentSortFunc)
    {
        var refreshedList = (await _assetService.PopulateAssetTotalsList()).Select(ModelMapper.MapToDisplay).ToList();

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

    private async Task UpdateListAssetTotals(Transaction transaction)
    {
        var selectedApiId = SelectedAssetApiId; // store the selected asset API ID before updating the list
        var updatedList = (await _assetService.UpdateListAssetTotals(transaction))
                .Select(ModelMapper.MapToDisplay)
                .ToList(); 

        ReplaceOrAddUpdatedItems(ListAssetTotals, updatedList, x => x.Coin.Id);
        if (!string.IsNullOrEmpty(selectedApiId))
        {
            var selectedItem = ListAssetTotals.FirstOrDefault(x => x.Coin.ApiId == selectedApiId);
            SelectedAssetApiId = selectedItem?.Coin.ApiId;
        }
    }


    private async Task UpdateListAssetTransactions(Transaction modifiedTransaction)
    {
        var updatedList = (await _transactionService.UpdateListAssetTransactions(modifiedTransaction))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        ReplaceOrAddUpdatedItems(ListAssetTransactions, updatedList, x => x.Id);
    }

    private async Task UpdateListAssetAccount(AssetAccount accountAffected)
    {
        var selectedId = SelectedAccountId;  
        var updatedList = await _accountService.UpdateListAssetAccount(accountAffected);
        ReplaceOrAddUpdatedItems(ListAssetAccounts, updatedList, x => x.AssetId);
        if (selectedId.HasValue)
        {
            var selectedItem = ListAssetAccounts.FirstOrDefault(x => x.AssetId == selectedId.Value);
            SelectedAccountId = selectedItem?.AssetId;
        }
    }

    private void ClearAccountsAndTransactions()
    {
        _accountService.ClearAccountsByAssetList();
        _transactionService.ClearAssetTransactionsList();

        ListAssetAccounts?.Clear();
        ListAssetTransactions?.Clear();
    }


    

}


