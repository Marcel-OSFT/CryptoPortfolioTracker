using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Serilog.Core;
using Windows.UI.Core;
using WinUI3Localizer;


namespace CryptoPortfolioTracker;

public partial class MainPage : Page, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static MainPage Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public IGraphUpdateService _graphUpdateService;
    public IPriceUpdateService _priceUpdateService;
    private readonly IPreferencesService _preferencesService;

    private Type lastPageType;
    private NavigationViewItem lastSelectedNavigationItem;
    private ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MainPage).Name.PadRight(22));
    

    public MainPage(PortfolioService portfolioService, IGraphUpdateService graphUpdateService, IPriceUpdateService priceUpdateService, IPreferencesService preferencesService)
    {
        InitializeComponent();
        Current = this;
        DataContext = this;

        _preferencesService = preferencesService;
        _graphUpdateService = graphUpdateService;
        _priceUpdateService = priceUpdateService;
        SetupTeachingTips();
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        _graphUpdateService.Start();
        _priceUpdateService.Start();

        //navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().First();
        navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().Where(x => x.Name == "AssetsView").First();
        App.Splash?.Close();
        App.Splash = null;

        if (_preferencesService.GetCheckingForUpdate())
        {
            await CheckUpdateNow();
        }
    }
    private void SetupTeachingTips()
    {
        var teachingTipInitial = _preferencesService.GetTeachingTip("TeachingTipBlank");
        var teachingTipNarr = _preferencesService.GetTeachingTip("TeachingTipNarrNavi");

        if (teachingTipInitial == null || !teachingTipInitial.IsShown)
        {
            _preferencesService.SetTeachingTipAsShown("TeachingTipNarrNavi");
            MyTeachingTipBlank1.IsOpen = true;
        }
        else if (teachingTipNarr != null && !teachingTipNarr.IsShown)
        {
            MyTeachingTipNarr.IsOpen = true;
        }
    }

    public async Task CheckUpdateNow()
    {
        Logger.Information("Checking for updates");
        var loc = Localizer.Get();
        AppUpdater appUpdater = new();
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
                //Download is running async, so the user can continue to do other stuff
                if (downloadResult == AppUpdaterResult.DownloadSuccesfull)
                {
                    //*** Download is doen, wait till there is no other dialog box open
                    await App.DialogCompletionTask;
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
    }

    private async void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        Type? pageType;
        if (args.IsSettingsSelected)
        {
            pageType = Type.GetType("CryptoPortfolioTracker.Views.SettingsView");
            if (pageType != null && pageType != lastPageType)
            {
                LoadView(pageType);
                lastSelectedNavigationItem = selectedItem;
                Logger.Information("Navigated to {0}", (string)selectedItem.Tag);
            }
        }
        else if (selectedItem != null)
        {
            pageType = Type.GetType("CryptoPortfolioTracker.Views." + (string)selectedItem.Tag);
            if (pageType is not null && pageType != lastPageType)
            {
                LoadView(pageType);
                lastSelectedNavigationItem = selectedItem;
                Logger.Information("Navigated to {0}", (string)selectedItem.Tag);
            }
            else if (pageType is null)
            {

                switch ((string)selectedItem.Tag)
                {
                    case "Exit":
                        {
                            Logger.Information("Application Exit");
                            Environment.Exit(0);
                            break;
                        }
                    case "Help":
                        {
                            DisplayHelpFile();
                            Logger.Information("Help File Requested");
                            break;
                        }
                    case "About":
                        {
                            var dialog = new AboutDialog(_preferencesService.GetAppTheme());
                            dialog.XamlRoot = MainPage.Current.XamlRoot;
                            var result = await dialog.ShowAsync();
                            break;
                        }
                }

                navigationView.SelectedItem = lastSelectedNavigationItem;
            }
        }
    }

    //private void LoadView(Type pageType)
    //{
    //    if (pageType.Name == "CoinLibraryView" || pageType.Name == "PriceLevelsView")
    //    {
    //        _graphUpdateService.Pause();
    //        _priceUpdateService.Pause();
    //    }
    //    else if (lastPageType is not null && (lastPageType.Name == "CoinLibraryView" || lastPageType.Name == "PriceLevelsView"))
    //    {
    //        if (_graphUpdateService != null)
    //        {
    //            _graphUpdateService.Resume();
    //        }
    //        else
    //        {
    //            _graphUpdateService = App.Container.GetService<IGraphUpdateService>();
    //            _graphUpdateService?.Start();
    //        }

    //        if (_priceUpdateService != null)
    //        {
    //            _priceUpdateService.Resume();
    //        }
    //        else
    //        {
    //            _priceUpdateService = App.Container.GetService<IPriceUpdateService>();
    //            _priceUpdateService?.Start();
    //        }
    //    }
    //    lastPageType = pageType;
    //    contentFrame.Content = App.Container.GetService(pageType);
    //}
    private void LoadView(Type pageType)
    {
        if (pageType.Name == "CoinLibraryView")
        {
            _graphUpdateService.Pause();
            _priceUpdateService.Pause();
        }
        else if (lastPageType is not null && (lastPageType.Name == "CoinLibraryView"))
        {
            if (_graphUpdateService != null)
            {
                _graphUpdateService.Resume();
            }
            else
            {
                _graphUpdateService = App.Container.GetService<IGraphUpdateService>();
                _graphUpdateService?.Start();
            }

            if (_priceUpdateService != null)
            {
                _priceUpdateService.Resume();
            }
            else
            {
                _priceUpdateService = App.Container.GetService<IPriceUpdateService>();
                _priceUpdateService?.Start();
            }
        }
        lastPageType = pageType;
        contentFrame.Content = App.Container.GetService(pageType);
    }

    private async void DisplayHelpFile()
    {
        var loc = Localizer.Get();
        var fileName = "HelpFile_NL.pdf";

        if (_preferencesService.GetAppCultureLanguage() == "en-US")
        {
            fileName = "HelpFile_EN.pdf";
        }
        try
        {
            Process.Start(new ProcessStartInfo(App.Url + fileName) { UseShellExecute = true });
            Logger.Information("HelpFile Displayed");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to display HelpFile");
            await ShowMessageDialog(
                loc.GetLocalizedString("Messages_HelpFile_FailedTitle"),
                loc.GetLocalizedString("Messages_HelpFile_FailedMsg"),
                loc.GetLocalizedString("Common_CloseButton"));
        }
    }

    public async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            XamlRoot = Current.XamlRoot,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            RequestedTheme = _preferencesService.GetAppTheme()
        };
        var dlgResult = await dialog.ShowAsync();
        return dlgResult;
    }

    private void OnGetItClickedNarr(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipNarr.IsOpen = false;
        _preferencesService.SetTeachingTipAsShown("TeachingTipNarrNavi");
        // Navigate to the new feature or provide additional information
    }
    private void OnGetItClickedBlank1(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipBlank1.IsOpen = false;
        MyTeachingTipBlank2.IsOpen = true;
        // Navigate to the new feature or provide additional information
    }
    private void OnGetItClickedBlank2(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipBlank2.IsOpen = false;
        MyTeachingTipBlank3.IsOpen = true;
        // Navigate to the new feature or provide additional information
    }
    private void OnGetItClickedBlank3(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipBlank3.IsOpen = false;
        _preferencesService.SetTeachingTipAsShown("TeachingTipBlank");
        // Navigate to the new feature or provide additional information
    }
    private void OnDismisslickedBlank1(object sender, RoutedEventArgs e)
    {
        MyTeachingTipBlank1.IsOpen = false;
        _preferencesService.SetTeachingTipAsShown("TeachingTipBlank");
    }
    private void OnDismisslickedBlank2(object sender, RoutedEventArgs e)
    {
        MyTeachingTipBlank2.IsOpen = false;
        _preferencesService.SetTeachingTipAsShown("TeachingTipBlank");
    }
    
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}

