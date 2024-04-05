using CryptoPortfolioTracker.Controls;
//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
//using CoinGecko.Interfaces;
//using CoinGecko.Parameters;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Popups;
//using PollyDemos.OutputHelpers;

namespace CryptoPortfolioTracker.Dialogs
{
    public partial class AddCoinDialog : ContentDialog, INotifyPropertyChanged
    {
        public readonly CoinLibraryViewModel _viewModel;
        public static AddCoinDialog Current;
        CoinFullDataById coinFullDataById;
        public Coin selectedCoin;

        private List<string> coinCollection;
        public List<string> CoinCollection
        {
            get { return coinCollection; }
            set
            {
                if (value != coinCollection)
                {
                    coinCollection = value;
                    OnPropertyChanged();
                }
            }
        }
        private string coinName;
        public string CoinName
        {
            get { return coinName; }
            set
            {
                if (value != coinName)
                {
                    coinName = value;
                    OnPropertyChanged();
                }
            }
        }

        private Visibility bePatientVisibility;
        public Visibility BePatientVisibility
        {
            get { return bePatientVisibility; }
            set
            {
                if (value != bePatientVisibility)
                {
                    bePatientVisibility = value;
                    OnPropertyChanged();
                }
            }
        }


        public AddCoinDialog(List<CoinList> coinList, CoinLibraryViewModel viewModel)
        {
            CoinName = "";
            Current = this;
            CoinCollection = coinList != null ? coinList.Select(x => x.Id).ToList() : new List<string>();
            //CoinCollection = coinList.ToList();

            this.InitializeComponent();
            _viewModel = viewModel;
            BePatientVisibility = Visibility.Collapsed;

        }


        // Handle user selecting an item, in our case just output the selected item.
        private async void AutoSuggestBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if ((sender as AutoSuggestBoxWithValidation).IsEntryMatched)
            {
                VisualStateRequestBusy(true);
                coinFullDataById = await GetCoinDetails(args.SelectedItem.ToString());
                VisualStateRequestBusy(false);
            }
        }

        private async void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter && (sender as AutoSuggestBoxWithValidation).IsEntryMatched)
            {
                VisualStateRequestBusy(true);
                coinFullDataById = await GetCoinDetails((sender as AutoSuggestBoxWithValidation).MyText);
                VisualStateRequestBusy(false);
            }
        }
        private void VisualStateRequestBusy(bool busy)
        {
            if (busy)
            {
                DetailsStack1.Opacity = 0.1;
                DetailsStack2.Opacity = 0.1;
                TheProgressRing.IsActive = true;
                //bePatientText.Visibility = Visibility.Visible;
            }
            else
            {
                TheProgressRing.IsActive = false;
                DetailsStack1.Opacity = 1;
                DetailsStack2.Opacity = 1;
                BePatientVisibility = Visibility.Collapsed;
            }
        }

        public void ShowBePatienceNotice()
        {
            BePatientVisibility = Visibility.Visible;
        }

        private void Button_Click_AddCoin(ContentDialog sender, ContentDialogButtonClickEventArgs e)
        {
            try
            {
                selectedCoin = null;
                var coin = new Coin
                {
                    //-- TO DO: checkk for things that could miss or be null
                    ApiId = coinFullDataById.Id.Length > 0 ? coinFullDataById.Id : null,
                    Name = coinFullDataById.Name.Length > 0 ? coinFullDataById.Name : null,
                    Symbol = coinFullDataById.Symbol.Length > 0 ? coinFullDataById.Symbol.ToUpper() : null,
                    ImageUri = coinFullDataById.Image.Small.AbsoluteUri.Length > 0 ? coinFullDataById.Image.Small.AbsoluteUri : null,
                    Price = (double)coinFullDataById.MarketData.CurrentPrice.Where(x => x.Key == "usd").SingleOrDefault().Value,
                    Ath = (double)coinFullDataById.MarketData.Ath.Where(x => x.Key == "usd").SingleOrDefault().Value,
                    Change52Week = (double)coinFullDataById.MarketData.PriceChangePercentage1YInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value,
                    Change1Month = (double)coinFullDataById.MarketData.PriceChangePercentage30DInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value,
                    MarketCap = (double)coinFullDataById.MarketData.MarketCap.Where(x => x.Key == "usd").SingleOrDefault().Value,
                    Change24Hr = (double)coinFullDataById.MarketData.PriceChangePercentage24HInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value,
                    About = (string)coinFullDataById.Description.Where(x => x.Key == "en").SingleOrDefault().Value,
                    IsAsset = false,
                    Rank = coinFullDataById.MarketCapRank != null ? (long)coinFullDataById.MarketCapRank : 99999
                };

                if (coin.ApiId != null && coin.Name != null && coin.Symbol != null)
                {
                    selectedCoin = coin;
                }
                else
                {
                    MessageDialog msgbox = new MessageDialog("Mandatory Coin data missing (Id/Name/Symbol). Coin will NOT be added to the Coin Library ");
                }
            }
            catch (Exception ex)
            {
                MessageDialog msgbox = new MessageDialog("Adding coin to the Library failed - " + ex.Message);
            }

        }

        public async Task<CoinFullDataById> GetCoinDetails(string coinId)
        {
            IsPrimaryButtonEnabled = false;

            CoinFullDataById coinDetails = null;
            var coinFullDataByIdResult = await _viewModel._libraryService.GetCoinDetails(coinId);
            coinFullDataByIdResult.IfSucc(async details =>
            {
                BitmapImage image = details.Image.Small.AbsoluteUri != null ? new BitmapImage(new Uri(base.BaseUri, details.Image.Small.AbsoluteUri)) : null;
                CoinImage.Source = image;

                Run run = new Run();
                run.Text = details.Id;
                parId.Inlines.Clear();
                parId.Inlines.Add(run);

                run = new Run();
                run.Text = details.Name;
                parName.Inlines.Clear();
                parName.Inlines.Add(run);

                run = new Run();
                run.Text = details.Symbol.ToUpper();
                parSymbol.Inlines.Clear();
                parSymbol.Inlines.Add(run);

                run = new Run();
                run.Text = details.MarketCapRank.ToString();
                parRank.Inlines.Clear();
                parRank.Inlines.Add(run);

                run = new Run();
                run.Text = details.MarketData.CurrentPrice.Where(x => x.Key == "usd").FirstOrDefault().Value.ToString();
                parPrice.Inlines.Clear();
                parPrice.Inlines.Add(run);

                if (await IsCoinAlreadyInLibrary(details.Id))
                {
                    PrimaryButtonText = "Already in Library";
                    IsPrimaryButtonEnabled = false;
                }
                else
                {
                    PrimaryButtonText = "Add Coin";
                    IsPrimaryButtonEnabled = true;
                }
                coinDetails = details;
            });
            return coinDetails;

        }
        public async Task<bool> IsCoinAlreadyInLibrary(string coinId)
        {
            var getCoinResult = await _viewModel._libraryService.GetCoin(coinId);
            return getCoinResult.Match(Succ: s => true, Fail: f => false);
        }

        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        //******* EventHandlers
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (MainPage.Current == null) return;
            MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            });
        }
    }
}

