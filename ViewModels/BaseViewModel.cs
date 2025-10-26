using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace CryptoPortfolioTracker.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    private protected ILogger Logger { get; set; }
    

    [ObservableProperty] private double? fontLevel1;
    [ObservableProperty] private double? fontLevel2;
    [ObservableProperty] private double? fontLevel3;

    [ObservableProperty]
    protected bool isScrollBarsExpanded;
    protected Settings AppSettings { get; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public BaseViewModel(Settings appSettings)
    {
        AppSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        IsScrollBarsExpanded = AppSettings.IsScrollBarsExpanded;
        SetFontLevels();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private void SetFontLevels()
    {
        switch (AppSettings.FontSize.ToString())
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
        var dialog = new ContentDialog()
        {
            Title = title,
            XamlRoot = MainPage.Current.XamlRoot,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            RequestedTheme = AppSettings.AppTheme,
        };
       
        var result = await App.ShowContentDialogAsync(dialog);
        return result;
    }

}