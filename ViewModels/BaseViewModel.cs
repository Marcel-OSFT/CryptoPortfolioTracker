using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Serilog;

namespace CryptoPortfolioTracker.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        private protected ILogger Logger
        {
            get; set;
        }


        [ObservableProperty] double fontLevel1;
        [ObservableProperty] double fontLevel2;
        [ObservableProperty] double fontLevel3;

        [ObservableProperty]
        protected bool isScrollBarsExpanded = App.userPreferences.IsScrollBarsExpanded;
        
        
        public BaseViewModel()
        {
            SetFontLevels();
        }

        private void SetFontLevels()
        {
            switch (App.userPreferences.FontSize.ToString())
            {
                case "Small":
                    {
                        FontLevel1 = 14;
                        FontLevel2 = 12;
                        FontLevel3 = 10;
                        break;
                    }
                case "Normal":
                    {
                        FontLevel1 = 16;
                        FontLevel2 = 14;
                        FontLevel3 = 12;

                        break;
                    }
                case "Large":
                    {
                        FontLevel1 = 18;
                        FontLevel2 = 16;
                        FontLevel3 = 14;

                        break;
                    }
                default: break;
            }

        }

        public async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                XamlRoot = MainPage.Current.XamlRoot,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                RequestedTheme=App.userPreferences.AppTheme
            };
            var dlgResult = await dialog.ShowAsync();
            return dlgResult;
        }

        public void Dispose()
        {
        }

    }
}