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

namespace CryptoPortfolioTracker.ViewModels;

public partial class PriceLevelsViewModel : BaseViewModel
{
    public IPriceLevelService _priceLevelService { get; private set; }
    private readonly IPreferencesService _preferencesService;
    [ObservableProperty] public string portfolioName = string.Empty;
    [ObservableProperty] public Portfolio currentPortfolio;

    partial void OnCurrentPortfolioChanged(Portfolio? oldValue, Portfolio newValue)
    {
        PortfolioName = newValue.Name;
    }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static PriceLevelsViewModel Current;
    [ObservableProperty] private string sortGroup;
    private SortingOrder initialSortingOrder;
    private Func<Coin, object> initialSortFunc;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public PriceLevelsViewModel(IPriceLevelService priceLevelService, IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PriceLevelsViewModel).Name.PadRight(22));
        Current = this;
        _priceLevelService = priceLevelService;
        _preferencesService = preferencesService;
        CurrentPortfolio = _priceLevelService.GetPortfolio();

        sortGroup = "Library";
        initialSortFunc = x => x.PriceLevels.Where(t => t.Type == PriceLevelType.TakeProfit).First().DistanceToValuePerc;
        initialSortingOrder = SortingOrder.Descending;
    }
    public async Task Initialize()
    {
        CurrentPortfolio = _priceLevelService.GetPortfolio();
        if (_priceLevelService.IsCoinsListEmpty())
        {
            await _priceLevelService.PopulateCoinsList(initialSortingOrder, initialSortFunc);
        }
    }

    public void Terminate()
    {
        
    }

    [RelayCommand]
    private void SortOnName(SortingOrder sortingOrder)
    {
        Func<Coin, string> sortFunc = x => x.Name;
        _priceLevelService.SortListString(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOnDistanceToTpLevel(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.PriceLevels.Where(t => t.Type==PriceLevelType.TakeProfit).First().DistanceToValuePerc;
        _priceLevelService.SortList(sortingOrder, sortFunc);
        //_priceLevelService.SortListTest(sortingOrder);
    }

    [RelayCommand]
    public void SortOnDistanceToBuyLevel(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.PriceLevels.Where(t => t.Type == PriceLevelType.Buy).First().DistanceToValuePerc;
        _priceLevelService.SortList(sortingOrder, sortFunc);
    }

    [RelayCommand]
    public void SortOnDistanceToStopLevel(SortingOrder sortingOrder)
    {
        Func<Coin, object> sortFunc = x => x.PriceLevels.Where(t => t.Type == PriceLevelType.Stop).First().DistanceToValuePerc;
        _priceLevelService.SortList(sortingOrder, sortFunc);
    }

    
    [RelayCommand]
    public async Task ShowAddLevelsDialog(Coin coin)
    {
        App.isBusy = true;
        var loc = Localizer.Get();
        Logger.Information("Showing Note Dialog");
        try
        {
            var dialog = new AddPriceLevelsDialog(coin, _preferencesService)
            {
                XamlRoot = PriceLevelsView.Current.XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Adding Price Levels for {0}", coin.Name);
                (await _priceLevelService.UpdatePriceLevels(coin, dialog.newPriceLevels))
                    .IfFail(async err =>
                    {
                        await ShowMessageDialog(
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

            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_PriceLevelsDialogFailed_Title"),
                ex.Message,
                loc.GetLocalizedString("Common_CloseButton"));
        }
        finally { App.isBusy = false; }
    }

    [RelayCommand(CanExecute = nameof(CanDeletePriceLevels))]
    public async Task DeletePriceLevels(Coin coin)
    {
        Logger.Information("Deleting price levels for coin {0}", coin.Name);
        var loc = Localizer.Get();
        var getResult = await _priceLevelService.ResetPriceLevels(coin);
            getResult.IfFail(async err =>
                   {
                       await ShowMessageDialog(
                          loc.GetLocalizedString("Messages_ResetLevelsFailed_Title"),
                          err.Message,
                          loc.GetLocalizedString("Common_CloseButton"));
                       Logger.Error(err, "Resetting levels failed");
                   });
    }
    private bool CanDeletePriceLevels(Coin coin)
    {
        var result = false;

        if (coin != null)
        {
            result = coin.PriceLevels.Any(x => x.Value != 0);
        }

        return result;
    }


    }

