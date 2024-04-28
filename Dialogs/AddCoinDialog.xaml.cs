using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Extensions;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

public partial class AddCoinDialog : ContentDialog, INotifyPropertyChanged
{
    private readonly DispatcherQueue dispatcherQueue;
    public readonly CoinLibraryViewModel _viewModel;
    public static AddCoinDialog Current;
    private CoinFullDataById? coinFullDataById;
    public Coin selectedCoin;
    private readonly ILocalizer loc = Localizer.Get();

    private List<string> coinCollection;
    public List<string> CoinCollection
    {
        get => coinCollection;
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
        get => coinName;
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
        get => bePatientVisibility;
        set
        {
            if (value != bePatientVisibility)
            {
                bePatientVisibility = value;
                OnPropertyChanged();
            }
        }
    }


    public AddCoinDialog(List<string> coinList, CoinLibraryViewModel viewModel)
    {
        this.dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        CoinName = "";
        Current = this;
        CoinCollection = coinList ?? new List<string>();
        this.InitializeComponent();
        _viewModel = viewModel;
        BePatientVisibility = Visibility.Collapsed;
        SetDialogTitleAndButtons();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("CoinDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("CoinDialog_PrimaryButton");
        CloseButtonText = loc.GetLocalizedString("CoinDialog_CloseButton");
        IsPrimaryButtonEnabled = false;
    }

    /// <summary>
    /// Handle user selecting an item, in our case just output the selected item.
    /// </summary>
    /// <param name="sender"></param>
    private async void AutoSuggestBox_SuggestionChosen(object sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (sender is AutoSuggestBoxWithValidation asBox && asBox.IsEntryMatched)
        {
            VisualStateRequestBusy(true);
            var selectedText = args.SelectedItem.ToString();
            coinFullDataById = selectedText is not null 
                ? await GetCoinDetails(selectedText.Split(",")[2].Trim())
                : null;
            VisualStateRequestBusy(false);
        }
    }

    private async void AutoSuggestBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter && (sender is AutoSuggestBoxWithValidation asBox) && asBox.IsEntryMatched)
        {
            VisualStateRequestBusy(true);
            coinFullDataById = await GetCoinDetails(asBox.MyText);
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
        Debug.WriteLine("set to visible");
    }

    private void Button_Click_AddCoin(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        try
        {
            var loc = Localizer.Get();
            if (coinFullDataById is not null) 
            {
                selectedCoin = null;
                var coin = new Coin
                {
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
                    var msgbox = new MessageDialog(loc.GetLocalizedString("Messages_CoinDataMissing"));
                }
            } 
            else
            {
                var msgbox = new MessageDialog(loc.GetLocalizedString("Messages_AddingCoinFailed"));            
            }
        }
        catch (Exception ex)
        {
            var msgbox = new MessageDialog(loc.GetLocalizedString("Messages_AddingCoinFailed" + ex.Message));
        }
    }

    public async Task<CoinFullDataById> GetCoinDetails(string coinId)
    {
        IsPrimaryButtonEnabled = false;
        CoinFullDataById coinDetails = null;

        try
        {
            var coinFullDataByIdResult = await _viewModel._libraryService.GetCoinDetails(coinId);
            coinFullDataByIdResult.IfSucc(async details =>
            {
                var image = details.Image.Small.AbsoluteUri != null ? new BitmapImage(new Uri(base.BaseUri, details.Image.Small.AbsoluteUri)) : null;
                CoinImage.Source = image;

                var run = new Run();
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
                    PrimaryButtonText = loc.GetLocalizedString("CoinDialog_PrimaryButton_CoinExists");
                    IsPrimaryButtonEnabled = false;
                }
                else
                {
                    PrimaryButtonText = loc.GetLocalizedString("CoinDialog_PrimaryButton");
                    IsPrimaryButtonEnabled = true;
                }
                coinDetails = details;
            });

        }
        catch (Exception)
        {
            var run = new Run();
            run.Text = loc.GetLocalizedString("CoinDialog_GetDetails_Failed");
            parId.Inlines.Clear();
            parId.Inlines.Add(run);
        }
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
    //public event PropertyChangedEventHandler PropertyChanged;
    //protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    //{
    //    if (MainPage.Current == null) return;
    //    MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
    //    {
    //        //Debug.WriteLine("OnPropertyChanged (BaseModel) => " + name);

    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null)
    //        {
    //            handler(this, new PropertyChangedEventArgs(propertyName));
    //        }
    //    });
    //}

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        this.dispatcherQueue.TryEnqueue(() =>
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        },
        exception =>
        {
            throw new Exception(exception.Message);
        });
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != App.userPreferences.AppTheme) sender.RequestedTheme = App.userPreferences.AppTheme;
    }
}

