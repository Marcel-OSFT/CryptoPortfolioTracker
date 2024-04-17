
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class CoinLibraryViewModel : BaseViewModel
{
    #region Fields related to the MVVM design pattern
    public static CoinLibraryViewModel Current;
    #endregion Fields related to the MVVM design pattern

    #region instances related to Services
    public readonly ILibraryService _libraryService;
    #endregion instances related to Services

    #region Fields and Proporties for DataBinding with the View

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowAddCoinDialogCommand))]
    bool isAllCoinDataRetrieved;
    
    [ObservableProperty] ObservableCollection<Coin> listCoins;

    public static AddCoinDialog dialog;

    #endregion variables and proporties for DataBinding with the View

    public static List<CoinList> coinListGecko;


    public CoinLibraryViewModel(ILibraryService libraryService)
    {
        //Logger = Log.Logger.ForContext<CoinLibraryViewModel>();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(CoinLibraryViewModel).Name.PadRight(22)); 
        Current = this;
        IsAllCoinDataRetrieved = false;
        _libraryService = libraryService;
        SetDataSource().ConfigureAwait(false);
        RetrieveAllCoinData();
    }
   

    #region MAIN methods or Tasks
    private async Task SetDataSource()
    {
        (await _libraryService.GetCoinsOrderedByRank())
            .IfSucc(list => ListCoins = new ObservableCollection<Coin>(list));
    }
    public async void RetrieveAllCoinData()
    {
        Logger.Information("Retrieved Coin List from CoinGecko");
        (await _libraryService.GetCoinListFromGecko())
            .IfSucc(list =>
            {
                coinListGecko = list;
                IsAllCoinDataRetrieved = coinListGecko.Count > 0 ? true : false;
            });
    }

    [RelayCommand(CanExecute = nameof(CanShowAddCoinDialog))]
    public async Task ShowAddCoinDialog()
    {
        Logger.Information("Showing Coin Dialog");
        App.isBusy = true;
        ILocalizer loc = Localizer.Get();
        try
        {
            dialog = new AddCoinDialog(coinListGecko, Current);
            dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Adding Coin to Library  - {0}", dialog.selectedCoin.Name);
                await (await _libraryService.CreateCoin(dialog.selectedCoin))
                    .Match(Succ: succ => AddToListCoins(dialog.selectedCoin),
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
        finally {App.isBusy = false;}
        
        App.isBusy = false;
    }
    private bool CanShowAddCoinDialog()
    {
        return IsAllCoinDataRetrieved;
    }

    [RelayCommand]
    public async Task ShowAddNoteDialog(Coin coin)
    {
        App.isBusy = true;
        ILocalizer loc = Localizer.Get();
        Logger.Information("Showing Note Dialog");
        try
        {
            AddNoteDialog dialog = new AddNoteDialog(coin);
            dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                Logger.Information("Adding Note for {0}", coin.Name);
                (await _libraryService.UpdateNote(coin, dialog.newNote))
                    .IfFail(async err => {
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
        finally {  App.isBusy = false;}
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteCoin))]
    public async Task DeleteCoin(Coin coin)
    {
        Logger.Information("Deleting coin {0}", coin.Name);
        ILocalizer loc = Localizer.Get();
        await (await _libraryService.RemoveCoin(coin))
           .Match(Succ: s => RemoveFromListCoins(coin),
                   Fail: async err => {
                       await ShowMessageDialog(
                          loc.GetLocalizedString("Messages_CoinDeleteFailed_Title"),
                          err.Message,
                          loc.GetLocalizedString("Common_CloseButton"));
                       Logger.Error(err, "Deleting coin failed");
                   });
    }
    private bool CanDeleteCoin(Coin coin)
    {
        bool result = false;
        try
        {
            result = !ListCoins.Where(x => x.ApiId.ToLower() == coin.ApiId.ToLower()).Single().IsAsset;
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
            DescriptionDialog dialog = new DescriptionDialog(coin);
            dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
            await dialog.ShowAsync();
        }
        App.isBusy = false;   
    }
    #endregion MAIN methods or Tasks

    #region SUB methods or Tasks
    private Task RemoveFromListCoins(Coin coin)
    {
        try
        {
            //var coin = ListCoins.Where(x => x.ApiId.ToLower() == coinId.ToLower()).Single();
            ListCoins.Remove(coin);
        }
        catch
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
    private Task AddToListCoins(Coin coin)
    {
        ListCoins.Add(coin);

        return Task.FromResult(true);
    }
    
    #endregion SUB methods or Tasks





}

