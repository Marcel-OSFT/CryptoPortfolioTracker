using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AddPrereleaseCoinDialog : ContentDialog
{
    public readonly CoinLibraryViewModel _viewModel;
    private readonly IPreferencesService _preferencesService;

    public Coin newCoin;
    private readonly ILocalizer loc = Localizer.Get();

    [ObservableProperty] private string decimalSeparator;
    


    public AddPrereleaseCoinDialog(CoinLibraryViewModel viewModel, IPreferencesService preferencesService)
    {
        InitializeComponent();
        _preferencesService = preferencesService;
        _viewModel = viewModel;
        DecimalSeparator = _preferencesService.GetNumberFormat().NumberDecimalSeparator;
        SetDialogTitleAndButtons();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("PreListingCoinDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_AcceptButton");
        CloseButtonText = loc.GetLocalizedString("Common_CancelButton");
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0;
    }

    private void Button_Click_AcceptCoin(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        var coin = new Coin
        {
            Name = CoinName.Text + "_pre-listing",
            Symbol = CoinSymbol.Text.ToUpper(),
            ApiId = CoinName.Text + CoinSymbol.Text,
            About = CoinAbout.Text,
            Price = Convert.ToDouble(CoinPrice.Text),
            ImageUri = App.appPath + "\\Assets\\QuestionMarkBlue.png",
            Rank = 999999
        };

        newCoin = coin;
    }

    private void CoinName_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0 && CoinSymbol.Text.Length > 0 && CoinPrice.IsEntryValid;
    }
    private void CoinSymbol_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0 && CoinSymbol.Text.Length > 0 && CoinPrice.IsEntryValid;
    }
    private void CoinPrice_Changed(object sender, System.EventArgs e)
    {
        IsPrimaryButtonEnabled = CoinName.Text.Length > 0 && CoinSymbol.Text.Length > 0 && CoinPrice.IsEntryValid;
    }

    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    
}
