using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class DashboardSettingsDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;

    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _preferencesService.SetExpandingScrollBars(value);

    [ObservableProperty]
    private int withinRangePerc;
    partial void OnWithinRangePercChanged(int value)
    {
        _preferencesService.SetWithinRangePerc(value);
        DashboardViewModel.Current?.RefreshTargetBubbles();
    }

    [ObservableProperty]
    private int closeToPerc;
    partial void OnCloseToPercChanged(int value) => _preferencesService.SetCloseToPerc(value);

    [ObservableProperty]
    private int maxPieCoins;
    partial void OnMaxPieCoinsChanged(int value)
    {
        _preferencesService.SetMaxPieCoins(value);
        DashboardViewModel.Current?.RefreshPortfolioPie();

    }
        [ObservableProperty]
    private int rsiPeriod;
    async partial void OnRsiPeriodChanged(int value)
    {
        _preferencesService.SetRsiPeriod(value);
        // update dashboard viewmodel display if available
        await DashboardViewModel.Current?.RefreshRsiBubbles();
    }

    [ObservableProperty]
    private int maPeriod;
   async partial void OnMaPeriodChanged(int value)
    {
        _preferencesService.SetMaPeriod(value);
        await DashboardViewModel.Current?.RefreshMaBubbles();
    }

    [ObservableProperty]
    private int maTypeIndex;
    partial void OnMaTypeIndexChanged(int value)
    {
        SetMaTypeByIndex(value);
        DashboardViewModel.Current?.RefreshMaBubbles();
    }

    public DashboardSettingsDialog(IPreferencesService preferencesService)
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
            sender.RequestedTheme = theme ;
        }
    }

    private void InitializeFields()
    {
        var preferences = _preferencesService;
        var numberFormat = preferences.GetNumberFormat();
        var appCulture = preferences.GetAppCultureLanguage();

        WithinRangePerc = preferences.GetWithinRangePerc();
        CloseToPerc = preferences.GetCloseToPerc();
        MaxPieCoins = preferences.GetMaxPieCoins();
        RsiPeriod = preferences.GetRsiPeriod();
        MaPeriod = preferences.GetMaPeriod();
        string maTypeValue = preferences.GetMaType();
        MaTypeIndex = maTypeValue switch
        {
            "SMA" => 0,
            "EMA" => 1,
            "WMA" => 2,
            "VWMA" => 3,
            _ => 0,
        };
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("DashboardSettingsDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }

    private void SetMaTypeByIndex(int index)
    {
        string value = index switch
        {
            0 => "SMA",
            1 => "EMA",
            2 => "WMA",
            3 => "VWMA",
            _ => "SMA",
        };
        _preferencesService.SetMaType(value);
    }


}
