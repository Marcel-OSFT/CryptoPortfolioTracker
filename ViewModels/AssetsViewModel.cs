
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
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Helpers;
using LanguageExt;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;
using static WinUI3Localizer.LanguageDictionary;
using LanguageExt.Common;
using WinRT;



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

    [ObservableProperty] private double totalAssetsValue;
    [ObservableProperty] private double totalAssetsCostBase;
    [ObservableProperty] private double totalAssetsPnLPerc;

    [ObservableProperty] private double inFlow;
    [ObservableProperty] private double outFlow;

   // [ObservableProperty] private static ObservableCollection<AssetTotals>? listAssetTotals;
    [ObservableProperty] private static ObservableCollection<AssetAccount>? listAssetAccounts;
    [ObservableProperty] private static ObservableCollection<Transaction>? listAssetTransactions;

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

    /// <summary>
    /// below commands need an intermediate binding because they are hidden in the ListView
    /// this is done to be able to force the commands to null when disposing the view
    /// </summary>
   
    //private SortingOrder currentSortingOrder;
    //private Func<AssetTotals, object> currentSortFunc;

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

        
    }

    /// <summary>
    /// SetDataSource async task is called from the View_Loading event of the associated View
    /// this to prevent to have it called from the ViewModels constructor
    /// </summary>
    public async Task Initialize()
    {
        Debug.WriteLine("SetDataSource - start");

        //var getAssetTotalsResult = await _assetService.GetAssetTotals();
        //getAssetTotalsResult.IfSucc(s => CreateListAssetTotals(s));
        //getAssetTotalsResult.IfFail(e => CreateListWithDummyAssetTotals());



        //ListAssetTotals = await _assetService.GetAssetTotalsList();
        await _assetService.PopulateAssetTotalsList();

        TotalAssetsValue = _assetService.GetTotalsAssetsValue();
        TotalAssetsCostBase = _assetService.GetTotalsAssetsCostBase();
        TotalAssetsPnLPerc = _assetService.GetTotalsAssetsPnLPerc();

        InFlow = await _assetService.GetInFlow();
        OutFlow = await _assetService.GetOutFlow();

        Debug.WriteLine("SetDataSource - end");



    }
    /// <summary>
    /// ReleaseDataSource async task is called from the View_UnLoaded event of the associated View
    /// </summary>
    public void Terminate()
    {
        Debug.WriteLine("ReleaseDataSource - start");

        //ListAssetTotals = MkOsft.NullObservableCollection<AssetTotals>(ListAssetTotals);
         //ListAssetAccounts = MkOsft.NullObservableCollection<AssetAccount>(ListAssetAccounts);
         //ListAssetTransactions = MkOsft.NullObservableCollection<Transaction>(ListAssetTransactions);

        selectedAccount = null;
        selectedAsset = null;
        IsExtendedView = false;

        _assetService.ClearAssetTotalsList();


        Debug.WriteLine("ReleaseDataSource - end");

    }

    //private Task Do_assetService.SortList(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
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

    //    currentSortingOrder = sortingOrder;
    //    currentSortFunc = sortFunc;

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



    [RelayCommand]
    public async Task AssetItemClicked(AssetTotals clickedAsset)
    {

        Debug.WriteLine("Selected asset: RC => " + clickedAsset.Coin.Name);

        //return;
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
                    .Match(Succ: newAsset => _assetService.UpdateListAssetTotals(dialog.transactionNew),
                    //.Match(Succ: newAsset => UpdateListAssetTotals(dialog.transactionNew),
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
                            // await UpdateListAssetTotals(transaction);
                            await _assetService.UpdateListAssetTotals(transaction);
                            
                             //await UpdateListAssetAccount(accountAffected);
                             await _accountService.UpdateListAssetAccount(accountAffected);
                             
                             // await RemoveFromListAssetTransactions(transaction);
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
        //(await _assetService.GetTransactionsByAsset(clickedAccount.AssetId))
        //    .IfSucc(s => CreateListAssetTransactions(s));

        await _transactionService.PopulateTransactionsByAssetList(clickedAccount.AssetId);
    }

    public async Task ShowAccountsAndTransactions(AssetTotals? clickedAsset = null)
    {
       
        if (clickedAsset != null)
        {
            //_assetService.IsSortingAfterUpdateEnabled = false;

            //(await _assetService.GetAccountsByAsset(clickedAsset.Coin.Id))
            //                    .IfSucc(s => CreateListAssetAcounts(s));
            
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

           // _assetService.IsSortingAfterUpdateEnabled = true;
        }

    }
    //public async Task ClearAccountsAndTransactions(AssetTotals clickedAsset)
    //{
    //    (await _assetService.GetAccountsByAsset(clickedAsset.Coin.Id))
    //            .IfSucc(s => CreateListAssetAcounts(s));
    //    if (ListAssetAccounts is not null && ListAssetAccounts.Count > 0)
    //    {
    //        var firstAccount = ListAssetAccounts.First();
    //        (await _assetService.GetTransactionsByAsset(firstAccount.AssetId))
    //            .IfSucc(s => CreateListAssetTransactions(s));
    //    }
    //}

    [RelayCommand]
    public void HideZeroBalances(bool param)
    {
        _assetService.IsHidingZeroBalances = param;
        
        //if (ListAssetTotals == null)
        //{
        //    return;
        //}

        //if (param)
        //{
        //    var itemsToHide = ListAssetTotals.Where(x => x.MarketValue <= 0).ToList();
        //    foreach (var item in itemsToHide)
        //    {
        //        item.IsHidden = true; ;
        //    }
        //}
        //else
        //{
        //    foreach (var item in ListAssetTotals)
        //    {
        //        item.IsHidden = false; ;
        //    }
        //}
    }

    //public async Task UpdateListAssetAccount(AssetAccount accountAffected)
   // {
        
        //if (ListAssetAccounts is null)
        //{
        //    return;
        //} 
        //(await _assetService.GetAccountByAsset(accountAffected.AssetId))
        //    .IfSucc(s =>
        //    {
        //        var index = -1;
                
        //        for (var i = 0; i < ListAssetAccounts.Count; i++)
        //        {
        //            if ( ListAssetAccounts[i].Name == accountAffected.Name)
        //            {
        //                index = i;
        //                break;
        //            }
        //        }
        //        if (index == -1)
        //        {
        //            return;
        //        }

        //        if (s != null && s.Name != string.Empty)
        //        {
        //            ListAssetAccounts[index] = s;
        //        }
        //        else
        //        {
        //            ListAssetAccounts.RemoveAt(index);
        //        }
        //    });
    //}

    //public async Task UpdateListAssetTotals(Transaction transaction)
   // {
        
        //if (ListAssetTotals is null)
        //{
        //    return;
        //}
        ////for updating purpose of the View, the affected elements of the data source List has to be updated
        ////*** First retrieve the coin(s) (max 2) affected by the transaction
        //var coinsAffected = transaction.Mutations.Select(x => x.Asset.Coin).Distinct().ToList();

        //// Check if one isn't in the assetsList yet, if so then add it.
        //foreach (var coin in coinsAffected)
        //{
        //    var assetAffected = ListAssetTotals.Where(x => x.Coin.Id == coin.Id).SingleOrDefault();
        //    if (assetAffected != null)
        //    {
        //        var index = -1;
        //        for (var i = 0; i < ListAssetTotals.Count; i++)
        //        {
        //            if (ListAssetTotals[i].Coin.Id == assetAffected.Coin.Id)
        //            {
        //                index = i;
        //                break;
        //            }
        //        }
        //        if (index >= 0)
        //        {
        //            var editedAT = (await _assetService.GetAssetTotalsByCoin(coin)).Match(Succ: s => s, Fail: err => new AssetTotals());
        //            if (editedAT.Coin is not null)
        //            {
        //                ListAssetTotals[index] = editedAT;
        //            }
        //        }
        //    }
        //    else //assetAffected == null
        //    {
        //        assetAffected = new AssetTotals();

        //        (await _assetService.GetAssetTotalsByCoin(coin)).IfSucc(s =>
        //        {
        //            assetAffected = s;
        //            ListAssetTotals.Add(assetAffected);
        //        });
        //    }
        //}
    //}
  //  public async Task UpdateListAssetTransaction(Transaction transactionNew, Transaction transactionToEdit)
  //  {
        

        //if (ListAssetTransactions is null)
        //{
        //    return Task.CompletedTask;
        //}

        //var index = -1;
        //for (var i = 0; i < ListAssetTransactions.Count; i++)
        //{
        //    if (ListAssetTransactions[i].Id == transactionToEdit.Id)
        //    {
        //        index = i;
        //        break;
        //    }
        //}
        //if (index >= 0)
        //{
        //    ListAssetTransactions[index] = transactionNew;
        //}

        //return Task.CompletedTask;
   // }
    //public async Task<bool> CreateListAssetTotals(List<AssetTotals> list)
    //{
        //ListAssetTotals = new ObservableCollection<AssetTotals>(list.OrderByDescending(x => x.MarketValue));

        //HideZeroBalances(IsHidingZeroBalances);
        //await CalculateAssetsTotalValues();

        //currentSortingOrder = SortingOrder.Descending;
        //currentSortFunc = new Func<AssetTotals, object>(x => x.MarketValue);

      //  return ListAssetTotals.Any();
    //}
    //public bool CreateListAssetAcounts(List<AssetAccount> list)
    //{
       // ListAssetAccounts = new ObservableCollection<AssetAccount>(list.OrderByDescending(x => x.Qty));
      //  return ListAssetAccounts.Any();
    //}
    //public bool CreateListAssetTransactions(List<Transaction> list)
    //{
        //TODO error caused below => look into XAML
       // return true;

      //  ListAssetTransactions = new ObservableCollection<Transaction>(list);
       
      //  return ListAssetTransactions.Any();
    //}
   // private bool CreateListWithDummyAssetTotals()
    //{
        //var dummyCoin = new Coin()
        //{
        //    Name = "EXCEPTIONAL ERROR",
        //    Symbol = "EXCEPTIONAL ERROR"
        //};
        //var dummyAssetTotals = new AssetTotals()
        //{
        //    Coin = dummyCoin
        //};

        //ListAssetTotals = new ObservableCollection<AssetTotals>
        //{
        //    dummyAssetTotals
        //};
      //  return ListAssetTotals.Any();
    //}

   // public Task<bool> RemoveFromListAssetTransactions(Transaction deletedTransaction)
   // {
        

        //if (ListAssetTransactions is null) { return Task.FromResult(false); }
        //var transactionToUpdate = ListAssetTransactions.Where(x => x.Id == deletedTransaction.Id).Single();
        //ListAssetTransactions.Remove(deletedTransaction);

     //   return Task.FromResult(true);
   // }

  //  public async Task CalculateAssetsTotalValues()
    //{
        //if (ListAssetTotals != null && ListAssetTotals.Count > 0 && ListAssetTotals[0].Coin.Symbol != "EXCEPTIONAL ERROR")
        //{
        //    TotalAssetsValue = ListAssetTotals.Sum(x => x.MarketValue);
        //    TotalAssetsCostBase = ListAssetTotals.Sum(x => x.CostBase);
        //    TotalAssetsPnLPerc = 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase;
        //}
        //await CalculateInAndOutFlow();
        //Logger.Information("CalculatingTotals {0}", TotalAssetsValue);
    //}

    //private async Task CalculateInAndOutFlow()
    //{
    //    InFlow = await _assetService.GetInFlow();
    //    OutFlow = await _assetService.GetOutFlow();
    //}

    //public void UpdatePricesAssetTotals(Coin coin, double oldPrice, double? newPrice)
    //{
    //    var asset = ListAssetTotals?.Where(a => a.Coin.Id == coin.Id).SingleOrDefault();
    //    if (asset != null && ListAssetTotals != null && oldPrice != newPrice)
    //    {
    //        //Logger.Information("Updating {0} {1} => {2}", coin.Name, oldPrice, newPrice);
    //        var index = -1;
    //        for (var i = 0; i < ListAssetTotals.Count; i++)
    //        {
    //            if (ListAssetTotals[i].Coin.Id == asset.Coin.Id)
    //            {
    //                index = i;
    //                break;
    //            }
    //        }
    //        ListAssetTotals[index].Coin = coin;
    //        ListAssetTotals[index].MarketValue = ListAssetTotals[index].Qty * coin.Price;
    //    }
    //    //apply/refresh sorting
    //    if (!IsExtendedView)
    //    {
    //        _assetService.SortList(currentSortingOrder, currentSortFunc);
    //    }

    //}

}

