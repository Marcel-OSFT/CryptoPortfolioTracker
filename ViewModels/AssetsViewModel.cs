
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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

//public class UpdatePricesMessage
//{
//    public Coin Coin { get; }
//    public UpdatePricesMessage(Coin coin)
//    {
//        Coin = coin;
//    }
//}

[ObservableRecipient]
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

    [ObservableProperty] public partial string PortfolioName { get; set; } = string.Empty;
    [ObservableProperty] public partial Portfolio CurrentPortfolio { get; set; }

    partial void OnCurrentPortfolioChanged(Portfolio oldValue, Portfolio newValue)
    {
        PortfolioName = newValue.Name;
    }

    [ObservableProperty] public partial string SortGroup { get; set; }

    public AssetTotals? selectedAsset = null;
    private AssetAccount? selectedAccount = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowTransactionDialogToAddCommand))]
    public partial bool IsAssetsExtendedView { get; set; } = false;

  
    [ObservableProperty] public partial bool IsHidingCapitalFlow { get; set; }

    public static List<CoinList>? coinListGecko;
    private bool disposedValue;

    private SortingOrder currentSortingOrder;
    private Func<AssetTotals, object> currentSortFunc;

    [ObservableProperty] public partial string GlyphPrivacy { get; set; } = "\uE890";

    [ObservableProperty] public partial bool IsPrivacyMode { get; set; }

    private NumberFormatInfo numberFormatInfo;

    partial void OnIsPrivacyModeChanged(bool value)
    {
        GlyphPrivacy = value ? "\uED1A" : "\uE890";
        _preferencesService.SetAreValuesMasked(value);
        _assetService.ReloadValues();
        _transactionService.ReloadValues();
        ReloadTotals();
    }

    [ObservableProperty] public partial double TotalPortfolioValue { get; set; }
    [ObservableProperty] public partial double TotalCostBase { get; set; }
    [ObservableProperty] public partial double TotalPnlPerc { get; set; }
    [ObservableProperty] public partial double TotalInflow { get; set; }
    [ObservableProperty] public partial double TotalOutflow { get; set; }
    [ObservableProperty] public partial long VisibleAssetsCount { get; set; }



    public AssetsViewModel(IGraphService graphService, 
            IAssetService assetService, 
            IAccountService accountService,
            ITransactionService transactionService,
            IMessenger messenger,
            IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(AssetsViewModel).Name.PadRight(22));
        Current = this;

        messenger.Register<UpdatePricesMessage>(this, async (r, m) =>
        {
            await _assetService.UpdatePricesAssetTotals(m.Coin);
            await GetPortfolioTotals();
        });

        messenger.Register<PortfolioConnectionChangedMessage>(this, async (r, m) =>
        {
            await LoadViewData();
        });
        messenger.Register<PreferencesChangedMessage>(this, async (r, m) =>
        {
            await RefreshViewAfterChangeOfPreferences();
        });


        _assetService = assetService;
        _transactionService = transactionService;
        _graphService = graphService;
        _preferencesService = preferencesService;
        _accountService = accountService;

        InitializeView();

    }

    /// <summary>
    /// Initialize async task is called from the View_Loaded event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    private async Task InitializeView()
    {
        CurrentPortfolio = _assetService.GetPortfolio();
        PortfolioName = CurrentPortfolio.Name;

        SortGroup = "Assets";
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;
        _assetService.IsHidingZeroBalances = _preferencesService.GetHidingZeroBalances();
        IsPrivacyMode = _preferencesService.GetAreValesMasked();
        numberFormatInfo = _preferencesService.GetNumberFormat();
        await LoadViewData();
    }

    
    public async Task ViewLoading()
    {
        await LoadViewData();
        //CurrentPortfolio = _assetService.GetPortfolio();
        //PortfolioName = CurrentPortfolio.Name;
     //   IsPrivacyMode = _preferencesService.GetAreValesMasked();

     //   IsHidingCapitalFlow = _preferencesService.GetHidingCapitalFlow();

       // await _assetService.PopulateAssetTotalsList(currentSortingOrder, currentSortFunc);

        //below setting(s) might have been changed while was moved away from the associated view

        //Numberformat might have changed!? So update below numbers to enforce immediate reflection of numberformat change
        //TotalPortfolioValue = _assetService.GetTotalsAssetsValue();
        //TotalCostBase = _assetService.GetTotalsAssetsCostBase();
        //TotalPnlPerc = _assetService.GetTotalsAssetsPnLPerc();

        //TotalInflow = await _assetService.GetInFlow();
        //TotalOutflow = await _assetService.GetOutFlow();
        //VisibleAssetsCount = _assetService.VisibleAssetsCount;

    }

    private async Task RefreshViewAfterChangeOfPreferences()
    {
        if (IsRelevantPreferenceChanged())
        {
            IsPrivacyMode = _preferencesService.GetAreValesMasked();
            IsHidingCapitalFlow = _preferencesService.GetHidingCapitalFlow();
            numberFormatInfo = _preferencesService.GetNumberFormat();

            //Numberformat might have changed!? So update below numbers to enforce immediate reflection of numberformat change
            await GetPortfolioTotals();

        }
    }

    private bool IsRelevantPreferenceChanged()
    {
        return IsPrivacyMode != _preferencesService.GetAreValesMasked()
             || IsHidingCapitalFlow != _preferencesService.GetHidingCapitalFlow()
             || numberFormatInfo != _preferencesService.GetNumberFormat();
    }

    private async Task LoadViewData()
    {
        CurrentPortfolio = _assetService.GetPortfolio();
        PortfolioName = CurrentPortfolio.Name;

        await _assetService.PopulateAssetTotalsList(currentSortingOrder, currentSortFunc);

        //below setting(s) might have been changed while was moved away from the associated view

        //Numberformat might have changed!? So update below numbers to enforce immediate reflection of numberformat change
        await GetPortfolioTotals();
    }

    private async Task GetPortfolioTotals()
    {
        TotalPortfolioValue = 0;
        TotalCostBase = 0;
        TotalPnlPerc = 0;
        TotalInflow = 0;
        TotalOutflow = 0;

        TotalPortfolioValue = _assetService.GetTotalsAssetsValue();
        TotalCostBase = _assetService.GetTotalsAssetsCostBase();
        TotalPnlPerc = _assetService.GetTotalsAssetsPnLPerc();
        TotalInflow = await _assetService.GetInFlow();
        TotalOutflow = await _assetService.GetOutFlow();
        VisibleAssetsCount = _assetService.VisibleAssetsCount;
    }


    /// <summary>
    /// ReleaseDataSource async task is called from the View_UnLoaded event of the associated View
    /// </summary>
    public void Terminate()
    {
        selectedAccount = null;
        selectedAsset = null;
        IsAssetsExtendedView = false;
       // _assetService.ClearAssetTotalsList();
        
    }

    private void ReloadTotals()
    {
        OnPropertyChanged(nameof(TotalPortfolioValue));
        OnPropertyChanged(nameof(TotalCostBase));
        OnPropertyChanged(nameof(TotalInflow));
        OnPropertyChanged(nameof(TotalOutflow));
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

    private void SortList(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    {
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
            
            IsAssetsExtendedView = true;
        }
        //clicked already selected item
        else if (selectedAsset != null && selectedAsset == clickedAsset)
        {
            if (!IsAssetsExtendedView)
            {
                await ShowAccountsAndTransactions(clickedAsset);
                
                IsAssetsExtendedView = true;
            }
            else
            {
                await ShowAccountsAndTransactions();
               
                IsAssetsExtendedView = false;
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
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Adding");
            var dialog = new TransactionDialog(_transactionService, _preferencesService, DialogAction.Add)
            {
                XamlRoot = AssetsView.Current.XamlRoot
            };
            var result = await App.ShowContentDialogAsync(dialog);

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
        
    }
    private bool CanShowTransactionDialogToAdd()
    {
        return !IsAssetsExtendedView;
    }

    [RelayCommand]
    public async Task ShowTransactionDialogToEdit(Transaction transaction)
    {
        AssetAccount? accountAffected = null;
        var loc = Localizer.Get();
        try
        {
            Logger.Information("Showing Transaction Dialog for Editing");
            //*** editing a transaction also involves a change for an element in the ListAssetAccounts
            accountAffected = _accountService.GetAffectedAccount(transaction);  //  ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();

            var dialog = new TransactionDialog(_transactionService, _preferencesService, DialogAction.Edit, transaction)
            {
                XamlRoot = AssetsView.Current.XamlRoot
            };
            var result = await App.ShowContentDialogAsync(dialog);

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
    }

    [RelayCommand]
    public async Task DeleteTransaction(Transaction transaction)
    {
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
                var accountAffected = _accountService.GetAffectedAccount(transaction);

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

    [RelayCommand]
    private void TogglePrivacyMode()
    {
        IsPrivacyMode = !IsPrivacyMode;
    }



}

