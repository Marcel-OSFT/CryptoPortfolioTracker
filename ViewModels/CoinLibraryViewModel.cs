
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.UI.Popups;
using LanguageExt;
using Microsoft.UI;
using CommunityToolkit.Common;

namespace CryptoPortfolioTracker.ViewModels
{
    public sealed partial class CoinLibraryViewModel : BaseViewModel
    {
        #region Fields related to the MVVM design pattern
        public static CoinLibraryViewModel Current;
        #endregion Fields related to the MVVM design pattern

        #region instances related to Services
        public readonly ILibraryService _libraryService;
        #endregion instances related to Services

        #region Fields and Proporties for DataBinding with the View
        private bool isAllCoinDataRetrieved;
        public bool IsAllCoinDataRetrieved
        {
            get { return isAllCoinDataRetrieved; }
            set
            {
                if (value != isAllCoinDataRetrieved)
                {
                    isAllCoinDataRetrieved = value;
                    OnPropertyChanged(nameof(IsAllCoinDataRetrieved));
                }
            }
        }
        public static AddCoinDialog dialog;

        private ObservableCollection<Coin> listCoins;
        public ObservableCollection<Coin> ListCoins
        {
            get { return listCoins; }
            set
            {
                if (value != listCoins)
                {
                    listCoins = value;
                    OnPropertyChanged(nameof(ListCoins));
                }
            }
        }
        #endregion variables and proporties for DataBinding with the View

        public static List<CoinList> coinListGecko;
            
        public CoinLibraryViewModel(ILibraryService libraryService)
        {
            Current = this;
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
            IsAllCoinDataRetrieved = false;
            (await _libraryService.GetCoinListFromGecko())
                .IfSucc(list => {
                    coinListGecko = list;
                    IsAllCoinDataRetrieved = coinListGecko.Count > 0 ? true : false;
                });
        }
        public async Task ShowAddCoinDialog()
        {
            Debug.WriteLine("entered Dialog ");

            dialog = new AddCoinDialog(coinListGecko, Current);
            dialog.XamlRoot = CoinLibraryView.Current.XamlRoot;
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                await (await _libraryService.CreateCoin(dialog.selectedCoin))
                    .Match(Succ: succ => AddToListCoins(dialog.selectedCoin),
                    Fail: async err => await ShowMessageBox("Adding coin failed - " + err.Message));
            }
            Debug.WriteLine("exited Dialog ");

        }
        public async Task ShowAddNoteDialog(string coinId)
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
                            .IfFail(async err => await ShowMessageBox("Updating note failed - " + err.Message));
                    }
                });
        }
        public async Task DeleteCoin(string coinId)
        {
            await (await _libraryService.RemoveCoin(coinId))
               .Match(Succ: s => RemoveFromListCoins(coinId),
                       Fail: async err => await ShowMessageBox("Failed to delete Coin"));
        }
        public async Task ShowDescription(string coinId)
        {
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
        }
        #endregion MAIN methods or Tasks

        #region SUB methods or Tasks
        private Task<bool> RemoveFromListCoins(string coinId)
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
        private Task<bool> AddToListCoins(Coin coin)
        {
            ListCoins.Add(coin);

            return Task.FromResult(true);
        }
        private async Task ShowMessageBox(string message, string primaryButtonText = "OK", string closeButtonText = "Close")
        {
            var dlg = new MsgBoxDialog(message);
            dlg.XamlRoot = CoinLibraryView.Current.XamlRoot;
            dlg.PrimaryButtonText = primaryButtonText;
            dlg.CloseButtonText = closeButtonText;
            await dlg.ShowAsync();
        }
        #endregion SUB methods or Tasks





    }

}

