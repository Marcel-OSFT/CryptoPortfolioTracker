using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class PortfolioSettingsDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;

    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    [ObservableProperty]
    private bool isHidingZeroBalances;
    partial void OnIsHidingZeroBalancesChanged(bool value) => _preferencesService.SetHidingZeroBalances(value);

    [ObservableProperty]
    private bool isHidingCapitalFlow;
    partial void OnIsHidingCapitalFlowChanged(bool value) => _preferencesService.SetHidingCapitalFlow(value);

    public PortfolioSettingsDialog(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
        InitializeComponent();
        DataContext = this;
        isCardExpanded = true;
        isCardEnabled = true;
        SetDialogTitleAndButtons();
        InitializeFields();
    }

    private async void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        var theme = _preferencesService.GetAppTheme();
        if (sender.ActualTheme != theme)
        {
            sender.RequestedTheme = theme;
        }
    }

    private void InitializeFields()
    {
        var preferences = _preferencesService;
        var numberFormat = preferences.GetNumberFormat();
        var appCulture = preferences.GetAppCultureLanguage();

        IsHidingZeroBalances = preferences.GetHidingZeroBalances();
        IsHidingCapitalFlow = preferences.GetHidingCapitalFlow();
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("PortfolioSettingsDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }




}

