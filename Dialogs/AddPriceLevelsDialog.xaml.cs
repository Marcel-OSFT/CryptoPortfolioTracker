using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Xml.Linq;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AddPriceLevelsDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;

    [ObservableProperty] private double tpValue;
    [ObservableProperty] private string tpNote = string.Empty;

    [ObservableProperty] private double buyValue;
    [ObservableProperty] private string buyNote =string.Empty;

    [ObservableProperty] private double stopValue;
    [ObservableProperty] private string stopNote = string.Empty;


    [ObservableProperty] private bool isValidTest = false;
   // [ObservableProperty] private Validator validator;
    [ObservableProperty] private string decimalSeparator;

    public ICollection<PriceLevel> newPriceLevels { get; set; }
     

    public AddPriceLevelsDialog(Coin coin, IPreferencesService preferencesService)
    {
        InitializeComponent();
       // DataContext = this;
        _preferencesService = preferencesService;

        DecimalSeparator = _preferencesService.GetNumberFormat().NumberDecimalSeparator;
        newPriceLevels = new List<PriceLevel>(); // Initialize newPriceLevels
        InitializeFields(coin.PriceLevels);

        SetDialogTitleAndButtons(coin);
    }


    private void InitializeFields(ICollection<PriceLevel> priceLevels)
    {
        //newPriceLevels = priceLevels;

        foreach (var level in priceLevels)
        {
            if (level.Type == PriceLevelType.TakeProfit)
            {
                TpValue = level.Value;
                TpNote = level.Note;
            }
            else if (level.Type == PriceLevelType.Buy)
            {
                BuyValue = level.Value;
                BuyNote = level.Note;
            }
            else if (level.Type == PriceLevelType.Stop)
            {
                StopValue = level.Value;
                StopNote = level.Note;
            }
        }
    }

    private void SetDialogTitleAndButtons(Coin coin)
    {
        Title = loc.GetLocalizedString("PriceLevelsDialog_AddTitle") + " " + coin.Name;
        PrimaryButtonText = loc.GetLocalizedString("PriceLevelsDialog_PrimaryButton");
        CloseButtonText = loc.GetLocalizedString("PriceLevelsDialog_CloseButton");
    }

    private void Button_Click_Accept(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        newPriceLevels = new List<PriceLevel>
        {
            new PriceLevel
            {
                Type = PriceLevelType.TakeProfit,
                Value = TpValue,
                Note = TpNote
            },
            new PriceLevel
            {
                Type = PriceLevelType.Buy,
                Value = BuyValue,
                Note = BuyNote
            },
            new PriceLevel
            {
                Type = PriceLevelType.Stop,
                Value = StopValue,
                Note = StopNote
            }
        };

        ////assign the new values to the price levels
        //foreach (var level in newPriceLevels)
        //{
        //    if (level.Type == PriceLevelType.TakeProfit)
        //    {
        //        level.Value = TpValue;
        //        level.Note = TpNote;
        //    }
        //    else if (level.Type == PriceLevelType.Buy)
        //    {
        //        level.Value = BuyValue;
        //        level.Note = BuyNote;
        //    }
        //    else if (level.Type == PriceLevelType.Stop)
        //    {
        //        level.Value = StopValue;
        //        level.Note = StopNote;
        //    }
        //}
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }
}


