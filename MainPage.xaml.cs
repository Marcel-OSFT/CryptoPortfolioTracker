using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;


namespace CryptoPortfolioTracker;

public partial class MainPage : Page
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static MainPage Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IServiceScope? _currentServiceScope;
    public LogWindow? logWindow;

    private ILogger Logger { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainPage()
    {
        ConfigureLogger();
        InitializeComponent();
        Current = this;
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private void ConfigureLogger()
    {
        if (App.isLogWindowEnabled)
        {
            logWindow = new LogWindow
            {
                Title = Assembly.GetExecutingAssembly().GetName().Name + " Event Log"
            };
            logWindow.Activate();
        }
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MainPage).Name.PadRight(22));
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
                    while (App.isBusy)
                    {
                        await Task.Delay(5000);
                        ;
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
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().First();
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var selectedItem = (NavigationViewItem)args.SelectedItem;
        Type? pageType;
        if (args.IsSettingsSelected)
        {
            pageType = Type.GetType("CryptoPortfolioTracker.Views.SettingsView");
            if (pageType != null)
            {
                LoadView(pageType);
            }

            Logger.Information("Navigated to {0}", "SettingsView");
        }
        else
        {
            if (selectedItem != null)
            {
                pageType = Type.GetType("CryptoPortfolioTracker.Views." + (string)selectedItem.Tag);
                if (pageType != null)
                {
                    LoadView(pageType);
                }

                Logger.Information("Navigated to {0}", (string)selectedItem.Tag); ;
            }
        }
    }
    private void LoadView(Type viewType)
    {
        _currentServiceScope?.Dispose();
        _currentServiceScope = App.Container.CreateAsyncScope();

        contentFrame.Content = _currentServiceScope.ServiceProvider.GetService(viewType);
    }
    public static async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            XamlRoot = Current.XamlRoot,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            RequestedTheme = App.userPreferences.AppTheme
        };
        var dlgResult = await dialog.ShowAsync();
        return dlgResult;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().First();
        App.Splash?.Close();
        if (App.userPreferences.IsCheckForUpdate) 
        { 
            await CheckUpdateNow(); 
        }
    }
}

