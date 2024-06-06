using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Serilog.Core;
using SQLitePCL;
using WinUI3Localizer;


namespace CryptoPortfolioTracker;

public partial class MainPage : Page, INotifyPropertyChanged
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static MainPage Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IServiceScope? _currentServiceScope;
   
    public IGraphUpdateService _graphUpdateService;
    public IPriceUpdateService _priceUpdateService;
    private readonly IPreferencesService _preferencesService;


    public LogWindow? logWindow;
    private Type lastPageType;
    private NavigationViewItem lastSelectedNavigationItem;

    private ILogger Logger { get; set; }


    private bool isChartLoaded;
    public bool IsChartLoaded
    {
        get { return isChartLoaded; }
        set
        {
            if (value != isChartLoaded)
            {
                isChartLoaded = value;
                OnPropertyChanged();
            }
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainPage(IGraphUpdateService graphUpdateService, IPriceUpdateService priceUpdateService, IPreferencesService preferencesService)
    {
        ConfigureLogger();
        InitializeComponent();
        Current = this;
        DataContext = this;

        _preferencesService = preferencesService;
        _graphUpdateService = graphUpdateService;
        _priceUpdateService = priceUpdateService;

        

    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private void ConfigureLogger()
    {
        if (App.isLogWindowEnabled)
        {
            logWindow = App.Container.GetService<LogWindow>();

            logWindow.Title = Assembly.GetExecutingAssembly().GetName().Name + " Event Log";
            
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

           // App.Splash?.Close();
           // App.Splash = null;

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
                if ((string)selectedItem.Tag == "Exit")
                {
                    Logger.Information("Application Exit");
                    Environment.Exit(0);
                }
                else if ((string)selectedItem.Tag == "Help")
                {
                    Logger.Information("Help File Requested");
                }
                navigationView.SelectedItem = lastSelectedNavigationItem;
            }
        }
    }

    private void LoadView(Type pageType)
    {
        _currentServiceScope?.Dispose();
        _currentServiceScope = null;
        _currentServiceScope = App.Container.CreateAsyncScope();

        if (pageType.Name == "CoinLibraryView" )
        {
            _graphUpdateService.Pause();
            _priceUpdateService.Pause();
        }
        else if (lastPageType is not null && lastPageType.Name == "CoinLibraryView")
        {
            _graphUpdateService.Continue();
            _priceUpdateService.Continue();
        }
        lastPageType = pageType;

        //contentFrame.Content = App.Container.GetService(pageType);
        contentFrame.Content = _currentServiceScope.ServiceProvider.GetService(pageType);
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

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        navigationView.SelectedItem = navigationView.MenuItems.OfType<NavigationViewItem>().First();
        App.Splash?.Close();
        App.Splash = null;

        if (_preferencesService.GetCheckingForUpdate()) 
        { 
            await CheckUpdateNow(); 
        }

        await _graphUpdateService.Start();
        _priceUpdateService.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


}

