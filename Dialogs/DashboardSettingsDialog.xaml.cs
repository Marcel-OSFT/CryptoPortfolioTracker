using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Threading.Tasks;
using System;
using WinUI3Localizer;
using System.Net.Http;
using System.Diagnostics;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Dialogs;

[ObservableObject]
public partial class DashboardSettingsDialog : ContentDialog
{
    private readonly ILocalizer loc = Localizer.Get();
    private readonly IPreferencesService _preferencesService;

    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;
    [ObservableProperty] DashboardSettings _dashboardSettings;

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _preferencesService.SetExpandingScrollBars(value);


    public DashboardSettingsDialog(DashboardSettings dashboardSettings, IPreferencesService preferencesService)
    {
        _dashboardSettings = dashboardSettings;
        _preferencesService = preferencesService;
        InitializeComponent();
        DataContext = this;
        isCardExpanded = false;
        isCardEnabled = true;
        SetDialogTitleAndButtons();
    }

    private async void Dialog_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        var theme = _preferencesService.GetAppTheme();
        if (sender.ActualTheme != theme)
        {
            sender.RequestedTheme = theme ;
        }
    }

    private void SetDialogTitleAndButtons()
    {
        Title = loc.GetLocalizedString("DashboardSettingsDialog_Title");
        PrimaryButtonText = loc.GetLocalizedString("Common_CloseButton");
        IsPrimaryButtonEnabled = true;
    }


    

}
