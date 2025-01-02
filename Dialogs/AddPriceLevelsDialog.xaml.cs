using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AddPriceLevelsDialog : ContentDialog
{
    public ICollection<PriceLevel> newPriceLevels { get; set; }
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;


    [ObservableProperty] private double tpValue;
    [ObservableProperty] private string tpNote;

    [ObservableProperty] private double buyValue;
    [ObservableProperty] private string buyNote;

    [ObservableProperty] private double stopValue;
    [ObservableProperty] private string stopNote;


    [ObservableProperty] private bool isValidTest = false;
    [ObservableProperty] private Validator validator;
    [ObservableProperty] private string decimalSeparator;



    public AddPriceLevelsDialog(Coin coin, IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferencesService = preferencesService;

        DecimalSeparator = _preferencesService.GetNumberFormat().NumberDecimalSeparator;
       // newPriceLevels = coin.PriceLevels;
        InitializeFields(coin.PriceLevels);

        SetDialogTitleAndButtons(coin);
    }


    private void InitializeFields(ICollection<PriceLevel> priceLevels)
    {
        newPriceLevels = priceLevels;

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
        Title = loc.GetLocalizedString("PriceLevelsDialog_Title") + " " + coin.Name;
        PrimaryButtonText = loc.GetLocalizedString("PriceLevelsDialog_PrimaryButton");
        CloseButtonText = loc.GetLocalizedString("PriceLevelsDialog_CloseButton");
    }

    private void Button_Click_Accept(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        //assign the new values to the price levels
        foreach (var level in newPriceLevels)
        {
            if (level.Type == PriceLevelType.TakeProfit)
            {
                level.Value = TpValue;
                level.Note = TpNote;
            }
            else if (level.Type == PriceLevelType.Buy)
            {
                level.Value = BuyValue;
                level.Note = BuyNote;
            }
            else if (level.Type == PriceLevelType.Stop)
            {
                level.Value = StopValue;
                level.Note = StopNote;
            }
        }
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }
}

