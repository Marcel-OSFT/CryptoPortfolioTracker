using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged, IDisposable
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static SettingsViewModel Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private readonly IPreferencesService _preferencesService;
    //private UserPreferences userPref;

    [ObservableProperty]
    private ElementTheme appTheme;
    partial void OnAppThemeChanged(ElementTheme value) => _preferencesService.SetAppTheme(value);

    [ObservableProperty]
    private int numberFormatIndex;
    partial void OnNumberFormatIndexChanged(int value) => SetNumberSeparators(value);
    
    [ObservableProperty]
    private int appCultureIndex;
    partial void OnAppCultureIndexChanged(int value) => SetCulturePreference(value);

    [ObservableProperty]
    private double fontSize;
    partial void OnFontSizeChanged(double value) => _preferencesService.SetFontSize((AppFontSize)value);

    [ObservableProperty]
    private bool isHidingZeroBalances;
    partial void OnIsHidingZeroBalancesChanged(bool value) => _preferencesService.SetHidingZeroBalances(value);

    [ObservableProperty]
    private bool isCheckForUpdate;
    partial void OnIsCheckForUpdateChanged(bool value) => _preferencesService.SetCheckingForUpdate(value);

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => _preferencesService.SetExpandingScrollBars(value);
    
    [ObservableProperty]
    private bool isHidingCapitalFlow;
    partial void OnIsHidingCapitalFlowChanged(bool value) => _preferencesService.SetHidingCapitalFlow(value);

    [ObservableProperty]
    private int withinRangePerc;
    partial void OnWithinRangePercChanged(int value) => _preferencesService.SetWithinRangePerc(value);

    [ObservableProperty]
    private int closeToPerc;
    partial void OnCloseToPercChanged(int value) => _preferencesService.SetCloseToPerc(value);
    
    [ObservableProperty]
    private int maxPieCoins;
    partial void OnMaxPieCoinsChanged(int value) => _preferencesService.SetMaxPieCoins(value);

    [ObservableProperty]
    private bool areValuesMasked;
    partial void OnAreValuesMaskedChanged(bool value) => _preferencesService.SetAreValuesMasked(value);


    public SettingsViewModel(IPreferencesService preferencesService) : base(preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(SettingsViewModel).Name.PadRight(22));
        Current = this;
        _preferencesService = preferencesService;
        InitializeFields();
    }

    public void Dispose()
    {
        Current = null!;
    }

    private void InitializeFields()
    {
        var preferences = _preferencesService;
        var numberFormat = preferences.GetNumberFormat();
        var appCulture = preferences.GetAppCultureLanguage();

        NumberFormatIndex = numberFormat.NumberDecimalSeparator == "," ? 0 : 1;
        AppCultureIndex = appCulture[..2].ToLower() == "nl" ? 0 : 1;
        IsHidingZeroBalances = preferences.GetHidingZeroBalances();
        IsCheckForUpdate = preferences.GetCheckingForUpdate();
        FontSize = (double)preferences.GetFontSize();
        IsScrollBarsExpanded = preferences.GetExpandingScrollBars();
        AppTheme = preferences.GetAppTheme();
        IsHidingCapitalFlow = preferences.GetHidingCapitalFlow();
        WithinRangePerc = preferences.GetWithinRangePerc();
        CloseToPerc = preferences.GetCloseToPerc();
        MaxPieCoins = preferences.GetMaxPieCoins();
    }

    private void SetNumberSeparators(int index)
    {
        var nf = new NumberFormatInfo
        {
            NumberDecimalSeparator = index == 0 ? "," : ".",
            NumberGroupSeparator = index == 0 ? "." : ","
        };
        _preferencesService.SetNumberFormat(nf);
    }
    private void SetCulturePreference(int index)
    {
        string culture = index == 0 ? "nl" : "en-us";
        _preferencesService.SetAppCultureLanguage(culture);
    }

    [RelayCommand]
    public async Task CheckUpdateNow()
    {
        Logger.Information("Checking for updates");
        var appUpdater = new AppUpdater();
        var loc = Localizer.Get();
        var result = await appUpdater.Check(App.VersionUrl, App.ProductVersion);

        if (result == AppUpdaterResult.NeedUpdate)
        {
            Logger.Information("Update Available");
            var dlgResult = await ShowMessageDialog(
                loc.GetLocalizedString("Messages_UpdateChecker_NewVersionTitle"),
                loc.GetLocalizedString("Messages_UpdateChecker_NewVersionMsg"),
                loc.GetLocalizedString("Common_DownloadButton"),
                loc.GetLocalizedString("Common_CancelButton"));

            if (dlgResult == ContentDialogResult.Primary)
            {
                Logger.Information("Downloading update");
                var downloadResult = await appUpdater.DownloadSetupFile();
                
                if (downloadResult == AppUpdaterResult.DownloadSuccesfull)
                {
                    //*** wait till there is no other dialog box open
                    while (App.isBusy)
                    {
                        await Task.Delay(5000);
                    }
                    Logger.Information("Download Succesfull");
                    var installRequest = await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadSuccesTitle"),
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadSuccesMsg"),
                        loc.GetLocalizedString("Common_InstallButton"),
                        loc.GetLocalizedString("Common_CancelButton"));

                    if (installRequest == ContentDialogResult.Primary)
                    {
                        Logger.Information("Closing Application and Installing Update");
                        appUpdater.ExecuteSetupFile();
                    }
                }
                else
                {
                    Logger.Warning("Download failed");
                    await ShowMessageDialog(
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadFailedTitle"),
                        loc.GetLocalizedString("Messages_UpdateChecker_DownloadFailedMsg"),
                        loc.GetLocalizedString("Common_CloseButton"));
                }
            }
        }
        else
        {
            Logger.Information("Application is up-to-date");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_UpdateChecker_UpToDate_Title"),
                loc.GetLocalizedString("Messages_UpdateChecker_UpToDate_Msg"),
                loc.GetLocalizedString("Common_OkButton"));
        }
    }
}

