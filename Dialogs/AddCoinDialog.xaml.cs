using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Animations.Expressions;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Extensions;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt.Common;
using LanguageExt.Pretty;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Popups;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;



[ObservableObject]
[ObservableRecipient]
public partial class AddCoinDialog : ContentDialog
{
    public readonly CoinLibraryViewModel _viewModel;
    private readonly IPreferencesService _preferencesService;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static AddCoinDialog Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private CoinFullDataById? coinFullDataById;
    public Coin? selectedCoin;
    
    private readonly DialogAction _dialogAction;
    private readonly ILocalizer loc = Localizer.Get();
    
    [ObservableProperty] private Visibility bePatientVisibility;
    [ObservableProperty] private List<string> coinList;
    [ObservableProperty] private List<Narrative> narratives;
    [ObservableProperty] private string coinName;
    //[ObservableProperty] private string selectedNarrative;
    [ObservableProperty] private Narrative initialNarrative = new();


    public AddCoinDialog(CoinLibraryViewModel viewModel, DialogAction dialogAction, IMessenger messenger,IPreferencesService preferencesService)
    {
        _viewModel = viewModel;
        CoinName = "";
        Current = this;
        _dialogAction = dialogAction;
        CoinList = _viewModel.searchListGecko ?? new List<string>();
        Narratives = _viewModel.narratives ?? new List<Narrative>();
        InitialNarrative = Narratives.Where(x => x.Name == "- Not Assigned -").FirstOrDefault();
        InitializeComponent();
        _preferencesService = preferencesService;
        BePatientVisibility = Visibility.Collapsed;
        SetDialogTitleAndButtons();
        messenger.Register<ShowBePatienceMessage>(this, (r, m) =>
        {
            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            {
                BePatientVisibility = Visibility.Visible;
            });
        });
    }

    private void SetDialogTitleAndButtons()
    {
        PrimaryButtonText = loc.GetLocalizedString("Common_AcceptButton");
        CloseButtonText = loc.GetLocalizedString("Common_CancelButton");
        IsPrimaryButtonEnabled = false;

        if (_dialogAction == DialogAction.Add)
        {
            Title = loc.GetLocalizedString("CoinDialog_Title");
        }
        else //merge
        {
            Title = loc.GetLocalizedString("CoinDialog_MergeTitle");
        }
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
            coinFullDataById = await GetCoinDetails(asBox.MyText.Split(",")[2].Trim());
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
    
    private void Button_Click_AddCoin(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        try
        {
            var loc = Localizer.Get();
            if (coinFullDataById is not null) 
            {
                selectedCoin = null;
                //var coin = new Coin
                //{
                //    ApiId = coinFullDataById.Id.Length > 0 ? coinFullDataById.Id : string.Empty,
                //    Name = coinFullDataById.Name.Length > 0 ? coinFullDataById.Name : string.Empty,
                //    Symbol = coinFullDataById.Symbol.Length > 0 ? coinFullDataById.Symbol.ToUpper() : string.Empty,
                //    ImageUri = coinFullDataById.Image.Small.AbsoluteUri.Length > 0 ? coinFullDataById.Image.Small.AbsoluteUri : string.Empty,
                //    Price = coinFullDataById.MarketData.CurrentPrice.Where(x => x.Key == "usd").SingleOrDefault().Value ?? 0,
                //    Ath = coinFullDataById.MarketData.Ath.Where(x => x.Key == "usd").SingleOrDefault().Value ?? 0,
                //    Change52Week = coinFullDataById.MarketData.PriceChangePercentage1YInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value,
                //    Change1Month = coinFullDataById.MarketData.PriceChangePercentage30DInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value,
                //    MarketCap = coinFullDataById.MarketData.MarketCap.Where(x => x.Key == "usd").SingleOrDefault().Value ?? 0,
                //    Change24Hr = coinFullDataById.MarketData.PriceChangePercentage24HInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value,
                //    About = coinFullDataById.Description.Where(x => x.Key == "en").SingleOrDefault().Value,
                //    IsAsset = false,
                //    Rank = coinFullDataById.MarketCapRank ?? 99999,
                    
                //};

                var coin = CoinBuilder.Create()
                    .WithApiId(coinFullDataById.Id.Length > 0 ? coinFullDataById.Id : string.Empty)
                    .WithName(coinFullDataById.Name.Length > 0 ? coinFullDataById.Name : string.Empty)
                    .WithSymbol(coinFullDataById.Symbol.Length > 0 ? coinFullDataById.Symbol.ToUpper() : string.Empty)
                    .WithImage(coinFullDataById.Image.Small.AbsoluteUri.Length > 0 ? coinFullDataById.Image.Small.AbsoluteUri : string.Empty)
                    .CurrentPriceAt(coinFullDataById.MarketData.CurrentPrice.Where(x => x.Key == "usd").SingleOrDefault().Value ?? 0)
                    .AllTimeHighAt(coinFullDataById.MarketData.Ath.Where(x => x.Key == "usd").SingleOrDefault().Value ?? 0)
                    .YearlyChangeAt(coinFullDataById.MarketData.PriceChangePercentage1YInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value)
                    .MonthlyChangeAt(coinFullDataById.MarketData.PriceChangePercentage30DInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value)
                    .DailyChangeAt(coinFullDataById.MarketData.PriceChangePercentage24HInCurrency.Where(x => x.Key == "usd").SingleOrDefault().Value)
                    .WithAbout(coinFullDataById.Description.Where(x => x.Key == "en").SingleOrDefault().Value)
                    .MarketCapRankAt(coinFullDataById.MarketCapRank ?? 99999)
                    .MarketCapAt(coinFullDataById.MarketData.MarketCap.Where(x => x.Key == "usd").SingleOrDefault().Value ?? 0)
                    .WithPriceLevel(priceLevel => priceLevel
                        .OfType(PriceLevelType.TakeProfit))
                    .WithPriceLevel(priceLevel => priceLevel
                        .OfType(PriceLevelType.Buy))
                    .WithPriceLevel(priceLevel => priceLevel
                        .OfType(PriceLevelType.Stop))
                    .WithPriceLevel(priceLevel => priceLevel
                        .OfType(PriceLevelType.Ema))
                    .OfNarrative(cbNarratives.SelectedItem as Narrative)
                    .Build();

                if (coin.ApiId != string.Empty && coin.Name != string.Empty && coin.Symbol != string.Empty)
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
        finally { Current = null!; }
    }

    public async Task<CoinFullDataById> GetCoinDetails(string coinId)
    {
        IsPrimaryButtonEnabled = false;
        CoinFullDataById coinDetails = new();
        var result = new Result<CoinFullDataById>();
        
        try
        {
            var coinFullDataByIdResult = await _viewModel._libraryService.GetCoinDetails(coinId);
            await coinFullDataByIdResult.Match(async details =>
            {
                DisplayCoinInfo(details);

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
            },
            err =>
            {
                result = new Result<CoinFullDataById>(err);
                return Task.CompletedTask;

            });
        }
        catch (Exception)
        {
            var run = new Run
            {
                Text = loc.GetLocalizedString("CoinDialog_GetDetails_Failed")
            };
            parId.Inlines.Clear();
            parId.Inlines.Add(run);
        }
        return coinDetails;
    }

    private void DisplayCoinInfo(CoinFullDataById details)
    {
        var image = details.Image.Small.AbsoluteUri != null ? new BitmapImage(new Uri(base.BaseUri, details.Image.Small.AbsoluteUri)) : null;
        CoinImage.Source = image;

        var run = new Run
        {
            Text = details.Id
        };
        parId.Inlines.Clear();
        parId.Inlines.Add(run);

        run = new Run
        {
            Text = details.Name
        };
        parName.Inlines.Clear();
        parName.Inlines.Add(run);

        run = new Run
        {
            Text = details.Symbol.ToUpper()
        };
        parSymbol.Inlines.Clear();
        parSymbol.Inlines.Add(run);

        run = new Run
        {
            Text = details.MarketCapRank.ToString()
        };
        parRank.Inlines.Clear();
        parRank.Inlines.Add(run);

        double? price = details.MarketData.CurrentPrice.Where(x => x.Key == "usd").FirstOrDefault().Value;
        run = new Run
        {
            Text = price?.ToString("0.########")
        };
        parPrice.Inlines.Clear();
        parPrice.Inlines.Add(run);
    }

    public async Task<bool> IsCoinAlreadyInLibrary(string coinId)
    {
        var getCoinResult = await _viewModel._libraryService.GetCoin(coinId);
        return getCoinResult.Match(Succ: s => true, Fail: f => false);
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    

}

