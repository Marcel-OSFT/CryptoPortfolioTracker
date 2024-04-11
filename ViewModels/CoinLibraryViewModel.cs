
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CryptoPortfolioTracker.ViewModels
{
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
            App.isBusy = true;
            try
            {
                dialog = new AddCoinDialog(coinListGecko, Current);
                dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    await (await _libraryService.CreateCoin(dialog.selectedCoin))
                        .Match(Succ: succ => AddToListCoins(dialog.selectedCoin),
                        Fail: async err => await ShowMessageDialog("Adding coin failed", err.Message, "Close"));
                }
            }
            catch (Exception ex)
            {
                await ShowMessageDialog("Add Coin Dialog failure", ex.Message, "Close");
            }
            finally {App.isBusy = false;}
            
            App.isBusy = false;
        }
        private bool CanShowAddCoinDialog()
        {
            return IsAllCoinDataRetrieved;
        }

        [RelayCommand]
        public async Task ShowAddNoteDialog(string coinId)
        {
            App.isBusy = true;
            try
            {
                (await _libraryService.GetCoin(coinId))
                    .IfSucc(async coin =>
                    {
                        AddNoteDialog dialog = new AddNoteDialog(coin);
                        dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
                        dialog.Title = "Add a note for " + coin.Name;
                        var result = await dialog.ShowAsync();
                        if (result == ContentDialogResult.Primary)
                        {
                            (await _libraryService.UpdateNote(coin, dialog.newNote))
                                .IfFail(async err => await ShowMessageDialog("Updating note failed", err.Message, "Close"));
                        }
                    });
            }
            catch (Exception ex)
            {
                await ShowMessageDialog("Note Dialog failure", ex.Message, "Close");
            }
            finally {  App.isBusy = false;}
        }
        
        [RelayCommand(CanExecute = nameof(CanDeleteCoin))]
        public async Task DeleteCoin(string coinId)
        {
            await (await _libraryService.RemoveCoin(coinId))
               .Match(Succ: s => RemoveFromListCoins(coinId),
                       Fail: async err => await ShowMessageDialog("Failed to delete Coin", err.Message, "Close"));
        }
        private bool CanDeleteCoin(string coinId)
        {
            bool result = false;
            try
            {
                result = !ListCoins.Where(x => x.ApiId.ToLower() == coinId.ToLower()).Single().IsAsset;
            }
            catch (Exception)
            {
                //Element just removed from the list...
            }
            return result;
        }


        [RelayCommand]
        public async Task ShowDescription(string coinId)
        {   
            App.isBusy = true;
            if (coinId != null)
            {
                (await _libraryService.GetCoin(coinId))
                    .IfSucc(async coin =>
                    {
                        DescriptionDialog dialog = new DescriptionDialog(coin.About);
                        dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
                        dialog.Title = "About " + coin.Name;
                        await dialog.ShowAsync();
                    });
            }
            App.isBusy = false;   
        }
        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks
        private Task RemoveFromListCoins(string coinId)
        {
            try
            {
                var coin = ListCoins.Where(x => x.ApiId.ToLower() == coinId.ToLower()).Single();
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

}

