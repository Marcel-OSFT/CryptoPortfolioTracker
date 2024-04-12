using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;
using Windows.Storage;
using CryptoPortfolioTracker.Extensions;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using Microsoft.Windows.ApplicationModel.Resources;

namespace CryptoPortfolioTracker.ViewModels;

public partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged
{
    public static SettingsViewModel Current;
    private readonly DispatcherQueue dispatcherQueue;


    private int numberFormatIndex;
    public int NumberFormatIndex
    {
        get => numberFormatIndex;
        set
        {
            if (value != numberFormatIndex)
            {
                numberFormatIndex = value;
                App.userPreferences.CultureLanguage = numberFormatIndex == 0 ? "nl" : "en";
                OnPropertyChanged();
            }
        }
    }
    private double fontSize;
    public double FontSize
    {
        get => fontSize;
        set
        {
            if (value != fontSize)
            {
                fontSize = value;
                App.userPreferences.FontSize = (AppFontSize)fontSize;
                OnPropertyChanged();
            }
        }
    }


    private bool isHidingZeroBalances;
    public bool IsHidingZeroBalances
    {
        get => isHidingZeroBalances;
        set
        {
            if (value != isHidingZeroBalances)
            {
                isHidingZeroBalances = value;
                App.userPreferences.IsHidingZeroBalances = isHidingZeroBalances;
                OnPropertyChanged();
            }
        }
    }


    private bool isCheckForUpdate;
    public bool IsCheckForUpdate
    {
        get => isCheckForUpdate;
        set
        {
            if (value != isCheckForUpdate)
            {
                isCheckForUpdate = value;
                App.userPreferences.IsCheckForUpdate = isCheckForUpdate;
                OnPropertyChanged();
            }
        }
    }

    private bool isScrollBarsExpanded;
    public bool IsScrollBarsExpanded
    {
        get => isScrollBarsExpanded;
        set
        {
            if (value != isScrollBarsExpanded)
            {
                isScrollBarsExpanded = value;
                App.userPreferences.IsScrollBarsExpanded = isScrollBarsExpanded;
                OnPropertyChanged();
            }
        }
    }

    public SettingsViewModel()
	{
		Current = this;
        this.dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        GetPreferences();
	}

    private void GetPreferences()
    {
        NumberFormatIndex = App.userPreferences.CultureLanguage == "nl" ? 0 : 1;
        IsHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;
        IsCheckForUpdate = App.userPreferences.IsCheckForUpdate;
        FontSize = (double)App.userPreferences.FontSize;
        IsScrollBarsExpanded = App.userPreferences.IsScrollBarsExpanded;
    }

    [RelayCommand]
    public async Task CheckUpdateNow()
    {
        AppUpdater appUpdater = new();
        ResourceLoader rl = new();
        var result = await appUpdater.Check(App.VersionUrl, App.ProductVersion);

        if (result == AppUpdaterResult.NeedUpdate)
        {
            var dlgResult = await ShowMessageDialog(
                rl.GetString("Messages_UpdateChecker_NewVersionTitle"), 
                rl.GetString("Messages_UpdateChecker_NewVersionMsg"), 
                rl.GetString("Common_DownloadButton"), 
                rl.GetString("Common_CancelButton"));

            if (dlgResult == ContentDialogResult.Primary)
            {
                var downloadResult = await appUpdater.DownloadSetupFile();
                if (downloadResult == AppUpdaterResult.DownloadSuccesfull)
                {
                    //*** Download is doen, wait till there is no other dialog box open
                    while (App.isBusy)
                    {
                        await Task.Delay(5000);
                        ;
                    }
                    var installRequest = await ShowMessageDialog(
                        rl.GetString("Messages_UpdateChecker_DownloadSuccesTitle"),
                        rl.GetString("Messages_UpdateChecker_DownloadSuccesMsg"),
                        rl.GetString("Common_InstallButton"),
                        rl.GetString("Common_CancelButton"));

                    if (installRequest == ContentDialogResult.Primary)
                    {
                        appUpdater.ExecuteSetupFile();
                        // Close application....
                        Environment.Exit(0);
                    }
                }
                else
                {
                    await ShowMessageDialog(
                        rl.GetString("Messages_UpdateChecker_DownloadFailedTitle"),
                        rl.GetString("Messages_UpdateChecker_DownloadFailedMsg"),
                        rl.GetString("Common_CloseButton"));
                }
            }
        }
        else
        {
            await ShowMessageDialog(
                rl.GetString("Messages_UpdateChecker_UpToDate_Title"),
                rl.GetString("Messages_UpdateChecker_UpToDate_Msg"),
                rl.GetString("Common_OkButton"));
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        this.dispatcherQueue.TryEnqueue(() =>
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        },
        exception =>
        {
            throw new Exception(exception.Message);
        });
    }

}

