using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;

namespace CryptoPortfolioTracker.ViewModels;

public partial class PriceLevelsViewModel : BaseViewModel, IRecipient<UpdatePricesMessage>
{
    public IPriceLevelService _priceLevelService { get; private set; }
    //public Settings AppSettings => base._appSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel
    private readonly IAppState _appState;

    public string PortfolioName => _appState.CurrentPortfolio.Name;

   // [ObservableProperty] public Portfolio currentPortfolio;
    [ObservableProperty] public partial long CoinsCount { get; set; }
    //partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    //{
    //    PortfolioName = newValue.Name;
    //}
    public static PriceLevelsViewModel Current;
    [ObservableProperty] private string sortGroup = "Library";
    private SortingOrder currentSortingOrder = SortingOrder.Descending;
    private Func<CoinDisplay, object> currentSortFunc = x => x.PriceLevels.Where(t => t.Type == PriceLevelType.TakeProfit).First().DistanceToValuePerc;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadingVisibility))]
    private bool isLoading;

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty] public partial string ExplainerText { get; set; }
    [ObservableProperty] public partial string ViewTitle { get; set; }

    [ObservableProperty] public partial ObservableCollection<CoinDisplay> ListCoins { get; set; } = new();    

    public PriceLevelsViewModel(IAppState appState, IPriceLevelService priceLevelService, IMessenger messenger, Settings appSettings, IDialogService dialogService) : base(appSettings, dialogService, messenger)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PriceLevelsViewModel).Name.PadRight(22));
        Current = this;
        _priceLevelService = priceLevelService;

        messenger.Register<UpdatePricesMessage>(this);
        _appState = appState;
        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(PortfolioName));
        };
    }

    public void Receive(UpdatePricesMessage message)
    {
        _ = RefreshCoinsListAsync();
    }


    public async Task ViewLoading()
    {
        try
        {
            ExplainerText = loc.GetLocalizedString("PriceLevels_Explainer");
            ViewTitle = loc.GetLocalizedString("PriceLevels_Title");
            IsLoading = true;

            await Task.Yield();

            await PopulateCoinsList();

        }
        catch (Exception ex)
        {
            // Your existing error handling
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Terminate()
    {
        _priceLevelService.ClearCoinsList();
        ListCoins.Clear();
    }

    [RelayCommand]
    private void SortOnName(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.Name);
    }

    [RelayCommand]
    public void SortOnDistanceToTpLevel(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.PriceLevels.Where(t => t.Type == PriceLevelType.TakeProfit).First().DistanceToValuePerc);
    }

    [RelayCommand]
    public void SortOnDistanceToBuyLevel(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.PriceLevels.Where(t => t.Type == PriceLevelType.Buy).First().DistanceToValuePerc);
    }

    [RelayCommand]
    public void SortOnDistanceToStopLevel(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.PriceLevels.Where(t => t.Type == PriceLevelType.Stop).First().DistanceToValuePerc);
    }

    [RelayCommand]
    public void SortOnDistanceToEmaLevel(SortingOrder sortingOrder)
    {
        SortList(sortingOrder, x => x.PriceLevels.Where(t => t.Type == PriceLevelType.Ema).First().DistanceToValuePerc);
    }

    private void SortList(SortingOrder sortingOrder, Func<CoinDisplay, object> sortFunc)
    {
        if (ListCoins is null || !ListCoins.Any()) return;

        currentSortFunc = sortFunc;
        currentSortingOrder = sortingOrder;

        if (currentSortingOrder == SortingOrder.Ascending)
        {
            ListCoins = new ObservableCollection<CoinDisplay>(ListCoins.OrderBy(currentSortFunc));
        }
        else
        {
            ListCoins = new ObservableCollection<CoinDisplay>(ListCoins.OrderByDescending(currentSortFunc));
        }
    }


    [RelayCommand]
    public async Task ShowAddLevelsDialog(CoinDisplay coin)
    {
        var loc = Localizer.Get();
        Logger.Information("Showing Price Levels Dialog");
        try
        {
            var domainCoin = ModelMapper.MapToDomain(coin);
            var newPriceLevels = await _dialogService.ShowAsync<AddPriceLevelsDialog, ICollection<PriceLevel>>(new AddPriceLevelsDialogParameters(domainCoin));

            if (newPriceLevels != null)
            {
                Logger.Information("Adding Price Levels for {0}", coin.Name);
                await (await _priceLevelService.UpdatePriceLevels(domainCoin, newPriceLevels))
                    .Match(
                        async updatedCoin =>
                        {
                            await UpdateCoinsList(updatedCoin);
                        },
                        async err =>
                        {
                            await _dialogService.ShowMessageAsync(
                                loc.GetLocalizedString("Messages_PriceLevelsAddFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Adding/Updating Price Levels failed");
                        });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Showing Price Levels Dialog failed");

            await _dialogService.ShowMessageAsync(
                loc.GetLocalizedString("Messages_PriceLevelsDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    [RelayCommand(CanExecute = nameof(CanResetPriceLevels))]
    public async Task ResetPriceLevels(CoinDisplay coin)
    {
        Logger.Information("Deleting price levels for coin {0}", coin.Name);
        var loc = Localizer.Get();
        var domainCoin = ModelMapper.MapToDomain(coin);
        await (await _priceLevelService.ResetPriceLevels(domainCoin))
                    .Match(
                        async updatedCoin =>
                        {
                            await UpdateCoinsList(domainCoin);
                        },
                        async err =>
                        {
                            await _dialogService.ShowMessageAsync(
                                loc.GetLocalizedString("Messages_ResetLevelsFailed_Title"),
                                err.Message,
                                loc.GetLocalizedString("Common_CloseButton"));
                            Logger.Error(err, "Resetting levels failed");
                        });
    }
    private bool CanResetPriceLevels(CoinDisplay coin)
    {
        var item = ListCoins.FirstOrDefault(x => x.Id == coin.Id);
        return item != null ? item.PriceLevels.Any(x => x.Value != 0) ? true : false : false;
    }

    private async Task PopulateCoinsList()
    {
        var list = (await _priceLevelService.PopulateCoinsList())
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        var sorted = currentSortingOrder == SortingOrder.Ascending
            ? list.OrderBy(currentSortFunc).ToList()
            : list.OrderByDescending(currentSortFunc).ToList();

        ListCoins = new ObservableCollection<CoinDisplay>(sorted);
        CoinsCount = ListCoins.Count;

    }

    private async Task RefreshCoinsListAsync()
    {
        var refreshedList = (await _priceLevelService.PopulateCoinsListAsync())
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        foreach (var item in refreshedList)
        {
            // Find the corresponding item in the existing collection and update its properties
            var existingItem = ListCoins?.FirstOrDefault(x => x.Id == item.Id);
            if (existingItem != null)
            {
                existingItem.PriceLevels = item.PriceLevels;
                existingItem.Price = item.Price;
                existingItem.Change1Month = item.Change1Month;
                existingItem.Change24Hr = item.Change24Hr;
            }
        }

    }

    private async Task UpdateCoinsList(Coin coin)
    {
        var updated = (await _priceLevelService.UpdateCoinsList(coin))
            .Select(ModelMapper.MapToDisplay)
            .ToList();

        ReplaceOrAddUpdatedItems(ListCoins, updated, x => x.Id);
    }


}

