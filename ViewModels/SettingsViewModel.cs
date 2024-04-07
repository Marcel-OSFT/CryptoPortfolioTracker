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
namespace CryptoPortfolioTracker.ViewModels
{
    public sealed partial class SettingsViewModel : BaseViewModel, INotifyPropertyChanged
    {
        public static SettingsViewModel Current;

        private int numberFormatIndex;
        public int NumberFormatIndex
        {
            get { return numberFormatIndex; }
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

        private bool isHidingZeroBalances;
        public bool IsHidingZeroBalances
        {
            get { return isHidingZeroBalances; }
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
            get { return isCheckForUpdate; }
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



        public SettingsViewModel()
		{
			Current = this;
            GetPreferences();
		}

        private void GetPreferences()
        {
            NumberFormatIndex = App.userPreferences.CultureLanguage == "nl" ? 0 : 1;
            IsHidingZeroBalances = App.userPreferences.IsHidingZeroBalances;
            IsCheckForUpdate = App.userPreferences.IsCheckForUpdate;
        }

        [RelayCommand]
        public async Task CheckUpdateNow()
        {
            AppUpdater appUpdater = new();
            var result = await appUpdater.Check(App.VersionUrl, App.ProductVersion);

            if (result == AppUpdaterResult.NeedUpdate)
            {
                var dlgResult = await ShowMessageDialog("Update Checker","New version available. Do you want to install it?", "Install", "Cancel");
                if (dlgResult == ContentDialogResult.Primary)
                {
                    appUpdater.Update();
                }
            }
            else
            {
                var dlgResult = await ShowMessageDialog("Update Checker", "Version is up-to-date", "OK");
            }
        }

        private async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                XamlRoot = SettingsView.Current.XamlRoot,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText
            };
            var dlgResult = await dialog.ShowAsync();
            return dlgResult;
        }


        //******* EventHandlers
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (MainPage.Current == null) return;
            MainPage.Current.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
            });
        }

    }

}

