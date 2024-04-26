using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Extensions;
using CryptoPortfolioTracker.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged
{
    public static SettingsViewModel Current;
    private readonly DispatcherQueue dispatcherQueue;

    [ObservableProperty]
    private ElementTheme appTheme;
    partial void OnAppThemeChanged(ElementTheme value) => App.userPreferences.AppTheme = value;

    [ObservableProperty]
    private int numberFormatIndex;
    partial void OnNumberFormatIndexChanged(int value) => SetNumberSeparators(value);
    
    [ObservableProperty]
    private int appCultureIndex;
    partial void OnAppCultureIndexChanged(int value) => SetCulturePreference(value);

    [ObservableProperty]
    private double fontSize;
    partial void OnFontSizeChanged(double value) => App.userPreferences.FontSize = (AppFontSize)value;

    [ObservableProperty]
    private bool isHidingZeroBalances;
    partial void OnIsHidingZeroBalancesChanged(bool value) => App.userPreferences.IsHidingZeroBalances = value;

    [ObservableProperty]
    private bool isCheckForUpdate;
    partial void OnIsCheckForUpdateChanged(bool value) => App.userPreferences.IsCheckForUpdate = value;

    [ObservableProperty]
    private bool isScrollBarsExpanded;
    partial void OnIsScrollBarsExpandedChanged(bool value) => App.userPreferences.IsScrollBarsExpanded = value;


    public SettingsViewModel()
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(SettingsViewModel).Name.PadRight(22));
        Current = this;
        this.dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        GetPreferences();
    }

    private void GetPreferences()
    {
        NumberFormatIndex = App.userPreferences.NumberFormat.CurrencyDecimalSeparator == "," ? 0 : 1;
        AppCultureIndex = App.userPreferences.AppCultureLanguage.Substring(0, 2).ToLower() == "nl" ? 0 : 1;
        IsHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;
        IsCheckForUpdate = App.userPreferences.IsCheckForUpdate;
        FontSize = (double)App.userPreferences.FontSize;
        IsScrollBarsExpanded = App.userPreferences.IsScrollBarsExpanded;
        AppTheme = App.userPreferences.AppTheme;
    }
    private void SetNumberSeparators(int index)
    {
        NumberFormatInfo nf = new();
        if (index == 0)
        {
            nf.CurrencyDecimalSeparator = ",";
            nf.CurrencyGroupSeparator = ".";
        }
        else
        {
            nf.CurrencyDecimalSeparator = ".";
            nf.CurrencyGroupSeparator = ",";
        }
        App.userPreferences.NumberFormat = nf;

    }
    private void SetCulturePreference(int index)
    {
        if (index == 0)
        {
            App.userPreferences.AppCultureLanguage = "nl";
        }
        else
        {
            App.userPreferences.AppCultureLanguage = "en-US";
        }
    }

    [RelayCommand]
    public async Task CheckUpdateNow()
    {
        Logger.Information("Checking for updates");
        AppUpdater appUpdater = new();
        ILocalizer loc = Localizer.Get();

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
                    //*** Download is doen, wait till there is no other dialog box open
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


    //public event PropertyChangedEventHandler PropertyChanged;
    //protected void OnPropertyChanged([CallerMemberName] string name = null)
    //{
    //    this.dispatcherQueue.TryEnqueue(() =>
    //    {
    //        PropertyChangedEventHandler handler = PropertyChanged;
    //        if (handler != null)
    //        {
    //            handler(this, new PropertyChangedEventArgs(name));
    //        }
    //    },
    //    exception =>
    //    {
    //        throw new Exception(exception.Message);
    //    });
    //}

}

