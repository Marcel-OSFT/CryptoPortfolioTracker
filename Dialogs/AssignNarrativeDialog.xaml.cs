using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Linq;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public sealed partial class AssignNarrativeDialog : ContentDialog
{
    private readonly IPreferencesService _preferencesService;
    private readonly CoinLibraryViewModel _viewModel;

    private readonly ILocalizer loc = Localizer.Get();

    [ObservableProperty] private List<Narrative> narratives;
    [ObservableProperty] private Narrative initialNarrative = new();
    public Narrative? selectedNarrative;
    private Coin _coin;


    public AssignNarrativeDialog(Coin coin, CoinLibraryViewModel viewModel, IPreferencesService preferencesService)
    {
        _coin = coin;
        InitializeComponent();
        _preferencesService = preferencesService;
        _viewModel = viewModel;
        Narratives = _viewModel.narratives ?? new List<Narrative>();
        InitialNarrative = _viewModel._narrativeService.GetNarrativeByCoin(coin);
        SetDialogTitleAndButtons();

    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("AssignNarrativeDialog_Title_Edit") + " " + _coin.Name;
        PrimaryButtonText = loc.GetLocalizedString("AssignNarrativeDialog_PrimaryButton_Edit");
        CloseButtonText = loc.GetLocalizedString("NarrativeDialog_CloseButton");
    }

    private void Button_Click_AcceptNarrative(ContentDialog sender, ContentDialogButtonClickEventArgs e)
    {
        selectedNarrative = cbNarratives.SelectedItem as Narrative;
    }


    private void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        if (sender.ActualTheme != _preferencesService.GetAppTheme())
        {
            sender.RequestedTheme = _preferencesService.GetAppTheme();
        }
    }

    
}

