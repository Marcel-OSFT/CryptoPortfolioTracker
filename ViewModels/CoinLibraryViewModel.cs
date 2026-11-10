using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class CoinLibraryViewModel : BaseViewModel
{
    private readonly ILibraryService _libraryService;
    private readonly INarrativeService _narrativeService;
    private readonly IAppState _appState;

    public static CoinLibraryViewModel Current { get; private set; }

    public Settings AppSettings => _appSettings;
    //private readonly List<Narrative> _narratives = new();
    public List<Narrative> Narratives { get; private set;  } = new();

    public List<string> SearchListGecko { get; private set; } = new();
    public List<CoinList> CoinListGecko { get; private set; } = new();
    [ObservableProperty] public partial ObservableCollection<CoinDisplay> CoinsList { get; set; } = new();

    public string PortfolioName => _appState.CurrentPortfolio?.Name ?? string.Empty;

    [ObservableProperty] public partial int? SelectedCoinId { get; set; }

    private Func<CoinDisplay, object> currentSortFunc = x => x.Rank;
    private SortingOrder currentSortingOrder = SortingOrder.Ascending;

    //[ObservableProperty]
    //private Portfolio currentPortfolio;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAddCoinDialogCommand))]
    private bool isAllCoinDataRetrieved;

    [ObservableProperty]
    private string sortGroup = "Library";

    [ObservableProperty]
    private long coinsCount;

    [ObservableProperty]
    private string explainerText = string.Empty;

    [ObservableProperty]
    private string viewTitle = string.Empty;

    public CoinLibraryViewModel(
        ILibraryService libraryService,
        INarrativeService narrativeService,
        Settings appSettings,
        IAppState appState,
        IDialogService dialogService,
        IMessenger messenger)
        : base(appSettings, dialogService, messenger)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(CoinLibraryViewModel).Name.PadRight(22));
        Current = this;

        _libraryService = libraryService;
        _narrativeService = narrativeService;
        _appState = appState;
        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PortfolioName));
            // reload data if needed
        };

        //CurrentPortfolio = _libraryService.GetPortfolio();
        SortGroup = "Library";
        IsAllCoinDataRetrieved = false;
    }

    //partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    //{
    //    if (newValue != null)
    //    {
    //        PortfolioName = newValue.Name;
    //    }
    //}

    public async Task ViewLoading()
    {
        ExplainerText = loc.GetLocalizedString("Library_Explainer");
        ViewTitle = loc.GetLocalizedString("Library_Title");
        await PopulateNarratives();
        await PopulateCoinsList();

        await RetrieveCoinSearchList();

    }

    private async Task PopulateNarratives()
    {
        var narrativesResult = await _narrativeService.GetNarratives();
        narrativesResult.IfSucc(list =>
        {
            Narratives = new List<Narrative>(list);
        });
    }

    public void Terminate()
    {
        SearchListGecko = MkOsft.NullList(SearchListGecko);
        CoinListGecko = MkOsft.NullList(CoinListGecko);
        Narratives = MkOsft.NullList(Narratives);
        CoinsList.Clear();
        _libraryService.ClearCoinsList();
    }

    public async Task RetrieveCoinSearchList()
    {
        Logger.Information("Retrieved Coin List from CoinGecko");

        var result = await _libraryService.GetCoinListFromGecko();

        result.IfSucc(list =>
        {
            CoinListGecko = list;
            IsAllCoinDataRetrieved = CoinListGecko.Count > 0;
            SearchListGecko = BuildSearchList(list);
        });
    }

    private static List<string> BuildSearchList(List<CoinList> list)
    {
        var searchList = new List<string>();
        foreach (var coin in list)
        {
            searchList.Add($"{coin.Name}, {coin.Symbol.ToUpper()}, {coin.Id}");
        }

        return searchList;
    }

    [RelayCommand]
    private void SortOnName(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Name);
    }

    [RelayCommand]
    private void SortOnNarrative(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Narrative.Name);
    }

    [RelayCommand]
    public void SortOn24HrChange(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Change24Hr);
    }

    [RelayCommand]
    public void SortOn52WeekChange(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Change52Week);
    }

    [RelayCommand]
    public void SortOn30DayChange(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Change1Month);
    }

    [RelayCommand]
    public void SortOnAth(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Ath);
    }

    [RelayCommand]
    public void SortOnMarketCap(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.MarketCap);
    }

    [RelayCommand]
    public void SortOnRank(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Rank);
    }

    private void SortList(SortingOrder sortingOrder, Func<CoinDisplay, object> sortFunc)
    {
        if (CoinsList is null || !CoinsList.Any()) return;

        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;

        if (currentSortingOrder == SortingOrder.Ascending)
        {
            CoinsList = new ObservableCollection<CoinDisplay>(CoinsList.OrderBy(currentSortFunc));
        }
        else
        {
            CoinsList = new ObservableCollection<CoinDisplay>(CoinsList.OrderByDescending(currentSortFunc));
        }
    }


    [RelayCommand(CanExecute = nameof(CanShowAddCoinDialog))]
    public async Task ShowAddCoinDialog()
    {
        //var narrativesResult = await _narrativeService.GetNarratives();
        //narrativesResult.IfSucc(list =>
        //{
        //    Narratives.Clear();
        //    Narratives.AddRange(list);
        //});

        Logger.Information("Showing Coin Dialog");

        var loc = Localizer.Get();

        try
        {
            var coin = await _dialogService.ShowAsync<AddCoinDialog, Coin>( new AddCoinDialogParameters(DialogAction.Add, SearchListGecko, Narratives));

            if (coin == null)
            {
                return;
            }

            var coinName = coin.Name;
            Logger.Information("Adding Coin to Library - {0}", coinName);

            var createResult = await _libraryService.CreateCoin(coin);

            await createResult.Match(
                Succ: async _ => await AddToCoinsList(coin),
                Fail: async err =>
                {
                    await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_CoinAddFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                    Logger.Error(err, "Adding Coin to Library Failed");
                });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Coin Dialog");

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_CoinDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    private bool CanShowAddCoinDialog()
    {
        return IsAllCoinDataRetrieved;
    }

    [RelayCommand(CanExecute = nameof(CanMergePreListingCoin))]
    public async Task MergePreListingCoin(CoinDisplay preListingCoin)
    {
        //var narrativesResult = await _narrativeService.GetNarratives();
        //narrativesResult.IfSucc(list =>
        //{
        //    Narratives.Clear();
        //    Narratives.AddRange(list);
        //});

        Logger.Information("Showing Coin Dialog");

        var loc = Localizer.Get();

        try
        {
            var coin = await _dialogService.ShowAsync<AddCoinDialog, Coin>(new AddCoinDialogParameters(DialogAction.Merge, SearchListGecko, Narratives));

            if (coin == null)
            {
                return;
            }

            var coinName = coin.Name;
            Logger.Information("Merging Coin in Library - {0}", coinName);

            var domainPreListingCoin = ModelMapper.MapToDomain(preListingCoin);
            var mergeResult = await _libraryService.MergeCoin(domainPreListingCoin, coin);

            await mergeResult.Match(
                Succ: _ => UpdateListCoins(domainPreListingCoin.Id),
                Fail: async err =>
                {
                    await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_CoinAddFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                    Logger.Error(err, "Merging Coin in Library Failed");
                });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Coin Dialog");

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_CoinDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    private bool CanMergePreListingCoin(CoinDisplay coin)
    {
        return coin.Name.Contains("_pre-listing");
    }

    [RelayCommand]
    public async Task ShowAddPreListingCoinDialog()
    {
        Logger.Information("Showing Pre-Listing Coin Dialog");

        var loc = Localizer.Get();

        try
        {
            var coin = await _dialogService.ShowAsync<AddPrereleaseCoinDialog, Coin?>();

            if (coin == null)
            {
                return;
            }

            var coinName = coin.Name;
            Logger.Information("Adding Pre-Listing Coin to Library - {0}", coinName);

            var createResult = await _libraryService.CreateCoin(coin);

            await createResult.Match(
                Succ: _ => AddToCoinsList(coin),
                Fail: async err =>
                {
                    await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_CoinAddFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                    Logger.Error(err, "Adding Pre-Listing Coin to Library Failed");
                });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show Pre-Listing Coin Dialog");

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_CoinDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    [RelayCommand]
    public async Task ShowAddNoteDialog(CoinDisplay coin)
    {
        var loc = Localizer.Get();
        Logger.Information("Showing Note Dialog");

        try
        {
            var domainCoin = ModelMapper.MapToDomain(coin);
            var note = await _dialogService.ShowAsync<AddNoteDialog, string>(new AddNoteDialogParameters(domainCoin));

            if (note == "canceled")
            {
                return;
            }

            Logger.Information("Adding Note for {0}", coin.Name);

            var updateResult = await _libraryService.UpdateNote(domainCoin, note);

            await updateResult.Match(
                Succ: _ => UpdateListCoins(coin.Id),
                Fail: async err =>
                {
                    await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_NoteAddFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                    Logger.Error(err, "Adding Note failed");
                });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing Note Dialog failed");

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_NoteDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    [RelayCommand(CanExecute = nameof(CanShowAssignNarrative))]
    public async Task ShowAssignNarrativeDialog(CoinDisplay coin)
    {
        var loc = Localizer.Get();
        Logger.Information("Showing AssignNarrative Dialog");

        try
        {
            var domainCoin = ModelMapper.MapToDomain(coin);
            //var narrativesResult = await _narrativeService.GetNarratives();
            //narrativesResult.IfSucc(list =>
            //{
            //    Narratives.Clear();
            //    Narratives.AddRange(list);
            //});

            var selectedNarrative = await _dialogService.ShowAsync<AssignNarrativeDialog, Narrative>(new AssignNarrativeDialogParameters(domainCoin, Narratives));

            if (selectedNarrative == null)
            {
                return;
            }

            Logger.Information("Assigning Narrative {0} to {1}", selectedNarrative.Name, coin.Name);

            var assignResult = await _narrativeService.AssignNarrative(domainCoin, selectedNarrative);

            await assignResult.Match(
                Succ: _ => UpdateListCoins(coin.Id),
                Fail: async err =>
                {
                    await _dialogService.ShowMessageAsync(
                        loc.GetLocalizedString("Messages_AssignNarrativeFailed_Title"),
                        err.Message,
                        loc.GetLocalizedString("Common_CloseButton"));

                    Logger.Error(err, "Assigning Narrative failed");
                });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing AssignNarrative Dialog failed");

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_AssignNarrativeDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    private bool CanShowAssignNarrative(CoinDisplay coin)
    {
        //*** prevent Stablecoins to get another narrative assigned
        var item = CoinsList.FirstOrDefault(x => x.Id == coin.Id);
        return item != null ? !coin.Narrative.Name.Contains("Stablecoin", StringComparison.OrdinalIgnoreCase) : false;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCoin))]
    public async Task DeleteCoin(CoinDisplay coin)
    {
        Logger.Information("Deleting coin {0}", coin.Name);

        var loc = Localizer.Get();
        var domainCoin = ModelMapper.MapToDomain(coin);
        var removeResult = await _libraryService.RemoveCoin(domainCoin);

        await removeResult.Match(
            Succ: _ => RemoveFromCoinsList(domainCoin),
            Fail: async err =>
            {
                await _dialogService.ShowMessageAsync(
                    loc.GetLocalizedString("Messages_CoinDeleteFailed_Title"),
                    err.Message,
                    loc.GetLocalizedString("Common_CloseButton"));

                Logger.Error(err, "Deleting coin failed");
            });
    }

    private bool CanDeleteCoin(CoinDisplay coin)
    {
        var item = CoinsList.FirstOrDefault(x => x.Id == coin.Id);
        return item != null ? item.IsAsset ? false : true : false;
    }

    [RelayCommand]
    public async Task ShowDescription(CoinDisplay coin)
    {
        Logger.Information("Showing Description Dialog for {0}", coin.Name);

        if (coin != null)
        {
            await _dialogService.ShowAsync<DescriptionDialog, bool>(new DescriptionDialogParameters(ModelMapper.MapToDomain(coin)));
        }
    }
    private async Task PopulateCoinsList()
    {
        var list = (await _libraryService.PopulateCoinsList())
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? list.OrderBy(currentSortFunc).ToList()
            : list.OrderByDescending(currentSortFunc).ToList();

        CoinsList = new ObservableCollection<CoinDisplay>(sorted);
        CoinsCount = CoinsList.Count;
    }

    private async Task UpdateListCoins(int coinId)
    {
        var selectedCoinId = SelectedCoinId; // store the selected asset API ID before updating the list
        var updated = (await _libraryService.UpdateCoinsList(coinId))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        ReplaceOrAddUpdatedItems(CoinsList, updated, x => x.Id);
        CoinsCount = CoinsList.Count;
        
        if (selectedCoinId.HasValue)
        {
            var selectedItem = CoinsList.FirstOrDefault(x => x.Id == selectedCoinId);
            SelectedCoinId = selectedItem?.Id;
        }
    }

    private async Task AddToCoinsList(Coin coin)
    {
        var result = await _libraryService.AddToCoinsList(coin);
        if (result)
        {
            CoinsList.Add(ModelMapper.MapToDisplay(coin));
            CoinsCount = CoinsList.Count;
        }
    }

    private async Task RemoveFromCoinsList(Coin coin)
    {
        var result = await _libraryService.RemoveFromCoinsList(coin);
        if (result)
        {
            var itemToRemove = CoinsList.FirstOrDefault(x => x.Id == coin.Id);
            if (itemToRemove != null)
            {
                CoinsList.Remove(itemToRemove);
                CoinsCount = CoinsList.Count;
            }
        }
    }



}