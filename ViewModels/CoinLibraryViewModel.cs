using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class CoinLibraryViewModel : BaseViewModel
{
    private readonly CoinLibraryViewModel _viewModel;
    
    public ILibraryService _libraryService { get; private set; }
    private readonly IPreferencesService _preferencesService;
    public readonly INarrativeService _narrativeService;
    [ObservableProperty] public string portfolioName = string.Empty;
    [ObservableProperty] public Portfolio currentPortfolio;

    partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    {
        PortfolioName = newValue.Name;
    }
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAddCoinDialogCommand))]
    private bool isAllCoinDataRetrieved;

   // [ObservableProperty] private ObservableCollection<Coin> listCoins = new();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public AddCoinDialog? dialog;
    public AddPrereleaseCoinDialog? preListingDialog;
    public static CoinLibraryViewModel Current;
    public List<string> searchListGecko;
    public List<CoinList> coinListGecko;
    [ObservableProperty] private string sortGroup;
    private SortingOrder initialSortingOrder;
    private Func<Coin, object> initialSortFunc;
    public List<Narrative> narratives;

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public CoinLibraryViewModel(ILibraryService libraryService, INarrativeService narrativeService ,IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(CoinLibraryViewModel).Name.PadRight(22));
        Current = this;
        IsAllCoinDataRetrieved = false;
        _libraryService = libraryService;
        _preferencesService = preferencesService;
        _narrativeService = narrativeService;
        _viewModel = this;

        CurrentPortfolio = _libraryService.GetPortfolio();

        sortGroup = "Library";
        initialSortFunc = x => x.Rank;
        initialSortingOrder = SortingOrder.Ascending;

        searchListGecko = new List<string>();
        coinListGecko = new List<CoinList>();
        narratives = new List<Narrative>();
    }
    public async Task Initialize()
    {
        CurrentPortfolio = _libraryService.GetPortfolio();
        if (_libraryService.IsCoinsListEmpty())
        {
            await _libraryService.PopulateCoinsList(initialSortingOrder, initialSortFunc);

        }
    }

    public void Terminate()
    {
        //Current = null;
        searchListGecko = MkOsft.NullList<string>(searchListGecko);
        coinListGecko = MkOsft.NullList<CoinList>(coinListGecko);

       // _libraryService.ClearCoinsList();
    }

    public async Task RetrieveAllCoinData()
    {
        Logger.Information("Retrieved Coin List from CoinGecko");

        (await _libraryService.GetCoinListFromGecko())
            .IfSucc(list =>
            {
                coinListGecko = list;
                IsAllCoinDataRetrieved = coinListGecko.Count > 0;
                searchListGecko = BuildSearchList(list);
            });
    }
    private static List<string> BuildSearchList(List<CoinList> list)
    {
        var searchList = new List<string>();
        foreach (var coin in list)
        {
            searchList.Add(coin.Name + ", " + coin.Symbol.ToUpper() + ", " + coin.Id); 
        }
        return searchList;
    }

    [RelayCommand]
    private void SortOnName(SortingOrder sortingOrder)
    {
        Func<Coin, string> sortFunc = x => x.Name;
        _libraryService.SortList(sortingOrder, sortFunc);
    }
    [RelayCommand]
    private void SortOnNarrative(SortingOrder sortingOrder)
    {
        Func<Coin, string> sortFunc = x => x.Narrative.Name;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOn24HrChange(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.Change24Hr;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOn52WeekChange(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.Change52Week;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOn30DayChange(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.Change1Month;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOnAth(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.Ath;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOnMarketCap(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.MarketCap;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOnRank(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.Rank;
        _libraryService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand(CanExecute = nameof(CanShowAddCoinDialog))]
    public async Task ShowAddCoinDialog()
    {
        (await _narrativeService.GetNarratives())
            .IfSucc(list => narratives = list);


        Logger.Information("Showing Coin Dialog");
        App.isBusy = true;
        var loc = Localizer.Get();
        try
        {
            dialog = new AddCoinDialog(Current, Enums.DialogAction.Add, App.Container.GetService<IMessenger>() ,_preferencesService)
            {
                XamlRoot = CoinLibraryView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var coinName = dialog.selectedCoin is not null ? dialog.selectedCoin.Name : string.Empty;
                Logger.Information("Adding Coin to Library  - {0}", coinName);
                await (await _libraryService.CreateCoin(dialog.selectedCoin))
                    .Match(Succ: succ => _libraryService.AddToCoinsList(dialog.selectedCoin),
                    Fail: async err =>
                    {
                        await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_CoinAddFailed_Title"),
                            err.Message,
                             loc.GetLocalizedString("Common_CloseButton"));
                        Logger.Error(err, "Adding Coin to Library Failed");
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Coin Dialog");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_CoinDialogFailed_Title"),
                ex.Message,
                 loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }

        App.isBusy = false;
    }
    private bool CanShowAddCoinDialog()
    {
        return IsAllCoinDataRetrieved;
    }

    [RelayCommand(CanExecute = nameof(CanMergePreListingCoin))]
    public async Task MergePreListingCoin(Coin preListingCoin)
    {
        Logger.Information("Showing Coin Dialog");
        App.isBusy = true;
        var loc = Localizer.Get();
        try
        {
            dialog = new AddCoinDialog(Current, Enums.DialogAction.Merge, App.Container.GetService<IMessenger>() ,_preferencesService)
            {
                XamlRoot = CoinLibraryView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var coinName = dialog.selectedCoin is not null ? dialog.selectedCoin.Name : string.Empty;
                Logger.Information("Merging Coin in Library  - {0}", coinName);
                await (await _libraryService.MergeCoin(preListingCoin, dialog.selectedCoin))
                    .Match(Succ: succ => UpdateListCoinsAfterMerge(preListingCoin, dialog.selectedCoin),
                    Fail: async err =>
                    {
                        await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_CoinAddFailed_Title"),
                            err.Message,
                             loc.GetLocalizedString("Common_CloseButton"));
                        Logger.Error(err, "Merging Coin in Library Failed");
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Coin Dialog");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_CoinDialogFailed_Title"),
                ex.Message,
                 loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }

        App.isBusy = false;
    }

    private bool CanMergePreListingCoin(Coin coin)
    {
        return coin.Name.Contains("_pre-listing");
    }

    [RelayCommand]
    public async Task ShowAddPreListingCoinDialog()
    {
        Logger.Information("Showing Pre-Listing Coin Dialog");
        App.isBusy = true;
        var loc = Localizer.Get();
        try
        {
            preListingDialog = new AddPrereleaseCoinDialog(Current, _preferencesService)
            {
                XamlRoot = CoinLibraryView.Current.XamlRoot
            };
            var result = await preListingDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var coinName = preListingDialog.newCoin is not null ? preListingDialog.newCoin.Name : string.Empty;
                Logger.Information("Adding Pre-Listing Coin to Library  - {0}", coinName);
                await (await _libraryService.CreateCoin(preListingDialog.newCoin))
                    .Match(Succ: succ =>_libraryService.AddToCoinsList(preListingDialog.newCoin),
                    Fail: async err =>
                    {
                        await ShowMessageDialog(
                            loc.GetLocalizedString("Messages_CoinAddFailed_Title"),
                            err.Message,
                             loc.GetLocalizedString("Common_CloseButton"));
                        Logger.Error(err, "Adding Pre-Listing Coin to Library Failed");
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Pre-Listing Coin Dialog");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_CoinDialogFailed_Title"),
                ex.Message,
                 loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }

        App.isBusy = false;
    }

    [RelayCommand]
    public async Task ShowAddNoteDialog(Coin coin)
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        Logger.Information("Showing Note Dialog");
        try
        {
            var dialog = new AddNoteDialog(coin, _preferencesService)
            {
                XamlRoot = CoinLibraryView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Adding Note for {0}", coin.Name);
                (await _libraryService.UpdateNote(coin, dialog.newNote))
                    .IfFail(async err =>
                    {
                        await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_NoteAddFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                        Logger.Error(err, "Adding Note failed");
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing Note Dialog failed");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_NoteDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }
    [RelayCommand]
    public async Task AssignNarrativeToCoin(Coin coin)
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        Logger.Information("Showing AssignNarrative Dialog");
        try
        {
            (await _narrativeService.GetNarratives())
            .IfSucc(list => narratives = list);

            var dialog = new AssignNarrativeDialog(coin, _viewModel ,_preferencesService)
            {
                XamlRoot = CoinLibraryView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Assigning Narrative {0} to {1}", dialog.selectedNarrative.Name, coin.Name);
                (await _narrativeService.AssignNarrative(coin, dialog.selectedNarrative))
                    .IfFail(async err =>
                    {
                        await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_AssignNarrativeFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                        Logger.Error(err, "Assigning Narrative failed");
                    });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing AssignNarrative Dialog failed");

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_AssignNarrativeDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCoin))]
    public async Task DeleteCoin(Coin coin)
    {
        Logger.Information("Deleting coin {0}", coin.Name);
        var loc = Localizer.Get();
        await (await _libraryService.RemoveCoin(coin))
           .Match(Succ: s => _libraryService.RemoveFromCoinsList(coin),
                   Fail: async err =>
                   {
                       await ShowMessageDialog(
                          loc.GetLocalizedString("Messages_CoinDeleteFailed_Title"),
                          err.Message,
                          loc.GetLocalizedString("Common_CloseButton"));
                       Logger.Error(err, "Deleting coin failed");
                   });
    }
    private bool CanDeleteCoin(Coin coin)
    {
        var result = false;
        try
        {
            //result = !ListCoins.Where(x => x.ApiId.ToLower() == coin.ApiId.ToLower()).Single().IsAsset;
            result = _libraryService.IsNotAsset(coin); 
        }
        catch (Exception)
        {
            //Element just removed from the list...
        }
        return result;
    }

    [RelayCommand]
    public async Task ShowDescription(Coin coin)
    {
        App.isBusy = true;
        Logger.Information("Showing Description Dialog for {0}}", coin.Name);
        if (coin != null)
        {
            var dialog = new DescriptionDialog(coin, _preferencesService)
            {
                XamlRoot = CoinLibraryView.Current.XamlRoot
            };
            await dialog.ShowAsync();
        }
        App.isBusy = false;
    }

    private Task UpdateListCoinsAfterMerge(Coin prelistingCoin, Coin? newCoin)
    {
        //if (newCoin == null || prelistingCoin == null) { return Task.FromResult(false); }
        //try
        //{
        //    var plCoin = ListCoins.Where(x => x.ApiId.ToLower() == prelistingCoin.ApiId.ToLower()).Single();
        //    plCoin.ApiId = newCoin.ApiId;
        //    plCoin.Name = newCoin.Name;
        //    plCoin.Symbol = newCoin.Symbol;
        //    plCoin.About = newCoin.About;

           
        //}
        //catch (Exception ex)
        //{
        //    return Task.FromResult(false); ;
        //}
        return Task.FromResult(true);
    }
}

