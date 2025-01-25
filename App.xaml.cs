using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Serilog;
using Serilog.Core;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using LanguageExt;
using System.Net.Http;

using WinUI3Localizer;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading;

//using Microsoft.UI.Xaml.Markup;
//using Microsoft.UI.Xaml;

namespace CryptoPortfolioTracker;

public partial class App : Application
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private Mutex _mutex;
    private const string MutexName = "MyUniqueWinUIMutex";
    private static ILogger? Logger;
    public static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);
    public static IPreferencesService PreferencesService { get; private set; }

    public static App Current { get; private set; }
    public static MainWindow? Window { get; private set; }
    public static Window? Splash { get; set; }
    public static string AppPath { get; private set; } = string.Empty;
    public static string AppDataPath { get; private set; } = string.Empty;
    public static string ProductVersion { get; private set; } = string.Empty;
    public static ILocalizer? Localizer { get; private set; }
    public static IServiceProvider Container { get; private set; }

    private static TaskCompletionSource<bool> dialogCompletionSource = new TaskCompletionSource<bool>();
    public static Task DialogCompletionTask => dialogCompletionSource.Task;

    //public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version_onedrive.txt";
    public const string Url = "https://marcel-osft.github.io/CryptoPortfolioTracker/";
    public const string CoinGeckoApiKey = "";
    public const string ApiPath = "https://api.coingecko.com/api/v3/";
    public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
    public const string DefaultPortfolioGuid = "f52ee1a8-ea8d-4f21-849c-6e6429f88256";

    public const string DbName = "sqlCPT.db";
    public const string PrefFileName = "prefs.xml";
    public const string BackupFolder = "Backup";
    public const string PrefixBackupName = "RestorePoint";
    public const string ExtentionBackup = "cpt";
    public const string IconsFolder = "LibraryIcons";
    public const string PortfoliosFileName = "portfolios.json";
    public static string PortfoliosPath { get; private set; }
    public static string ChartsFolder { get; private set; }

    public App()
    {
        Current = this;
        InitializeComponent();
        this.UnhandledException += OnUnhandledException;

        GetAppEnvironmentals();
        Container = RegisterServices();
        PreferencesService = Container.GetService<IPreferencesService>() ?? throw new InvalidOperationException("Failed to retrieve IPreferencesService from the service container.");
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Splash = new SplashScreen();
        Splash.Activate();
        await Task.Delay(1000);

        // Create or open the mutex
        _mutex = new Mutex(false, MutexName, out bool createdNew);

        // Check if a new mutex was created
        if (!createdNew)
        {
            // Another instance is already running
            await ShowErrorMessage("Another instance of the application is already running.");
            _mutex.Close();
            _mutex = null;

            // Exit the current application
            Application.Current.Exit();
            return;
        }

        PreferencesService.LoadUserPreferencesFromXml();

        AddNewTeachingTips();
        
        InitializeLogger();
        await InitializeLocalizer();

        InitializePortfolioService();
        
        Logger?.Information("Start to CacheIcons.");
        
        CacheLibraryIconsAsync();
        
        Logger?.Information("CacheIcons done.");
        
        Logger?.Information("GetService<MainWindow>");
        Window = Container.GetService<MainWindow>();
        if (Window != null)
        {
            Window.Activate();
        }
        else
        {
            Logger?.Error("Failed to retrieve MainWindow from the service container.");
        }
        Logger?.Information("MainWindow Activated");

    }

    private static async void CacheLibraryIconsAsync()
    {
        await CacheLibraryIcons();
    }
    private static async Task CacheLibraryIcons()
    {
        var iconsFolderPath = Path.Combine(AppDataPath, IconsFolder);
        if (Directory.Exists(iconsFolderPath))
        {
            foreach (var file in Directory.GetFiles(iconsFolderPath))
            {
                File.Delete(file);
            }
        }
        else
        {
            Directory.CreateDirectory(iconsFolderPath);
        }

        var portfolioService = App.Container.GetService<PortfolioService>();

        var context = portfolioService.Context;


        var coins = context?.Coins.Where(coin => !string.IsNullOrEmpty(coin.ImageUri)).ToList();

        if (coins != null)
        {
            var tasks = coins.Select(async coin =>
            {
                var fileName = ExtractFileNameFromUri(coin.ImageUri);
                if (fileName != "QuestionMarkBlue.png")
                {
                    var iconPath = Path.Combine(iconsFolderPath, fileName);
                    if (!File.Exists(iconPath))
                    {
                        if (!await RetrieveCoinIconAsync(coin, iconPath))
                        {
                            Logger?.Warning("Failed to cache icon for {0}", coin.Name);
                        }
                    }
                }
                
            });

            await Task.WhenAll(tasks);
        }
    }

    private async static Task<bool> RetrieveCoinIconAsync(Coin? coin, string iconPath)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(coin?.ImageUri);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using var fs = new FileStream(iconPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            return true;
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Failed to retrieve coin icon for {0}", coin?.Name);
            return false;
        }
    }

    private static string ExtractFileNameFromUri(string uri)
    {
        var uriWithoutQuery = uri.Split('?')[0];
        return Path.GetFileName(uriWithoutQuery);
    }

    private static void AddNewTeachingTips()
    {
        var tips = new List<TeachingTipCPT>
        {
            //*** version 1.2.7
            new() { Name = "TeachingTipNarrLibr", IsShown = false },
            new() { Name = "TeachingTipNarrDash", IsShown = false },
            new() { Name = "TeachingTipNarrNarr", IsShown = false },
            new() { Name = "TeachingTipPortDash", IsShown = false },
            new() { Name = "TeachingTipNarrNavi", IsShown = false },

            //*** version 1.2.9
            new() { Name = "TeachingTipRsiHeat", IsShown = false },

        };

        PreferencesService.AddTeachingTipsIfNotExist(tips);
    }

    private async static Task InitializeLocalizer()
    {
        var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
            .Build();

        var culture = PreferencesService.GetAppCultureLanguage();
        Logger?.Information("Setting Language to {0}", culture);

        try
        {
            await Localizer.SetLanguage(culture);
            Logger?.Information("Language set successfully.");
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Failed to set language.");
        }
    }


    private static async void InitializePortfolioService()
    {
        var contextService = App.Container.GetService<PortfolioService>();
        await contextService.InitializeAsync();
    }


    private static void GetAppEnvironmentals()
    {
        AppPath = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? string.Empty;
        AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CryptoPortfolioTracker";
        if (!Directory.Exists(AppDataPath))
        {
            Directory.CreateDirectory(AppDataPath);
        }

        AppDomain.CurrentDomain.SetData("DataDirectory", AppDataPath);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        ProductVersion = version is not null ? version.ToString() : string.Empty ;

        PortfoliosPath = Path.Combine(AppDataPath, "Portfolios");
        ChartsFolder = Path.Combine(AppDataPath, "MarketCharts");


    }
    private static IServiceProvider RegisterServices()
    {
        var services = new ServiceCollection();

        services.AddScoped<AssetsView>();
        services.AddScoped<AccountsView>();
        services.AddScoped<CoinLibraryView>();
        services.AddScoped<SettingsView>();
        services.AddScoped<MainPage>();
        services.AddScoped<MainWindow>();
        services.AddScoped<DashboardView>();
        services.AddScoped<PriceLevelsView>();
        services.AddScoped<NarrativesView>();
        services.AddScoped<SwitchPortfolioView>();
        services.AddScoped<AdminView>();


        services.AddScoped<AssetsViewModel>();
        services.AddScoped<AccountsViewModel>();
        services.AddScoped<CoinLibraryViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<PriceLevelsViewModel>();
        services.AddScoped<BaseViewModel>();
        services.AddScoped<NarrativesViewModel>();
        services.AddScoped<SwitchPortfolioViewModel>();
        services.AddScoped<AdminViewModel>();



        // Register the factory
        services.AddSingleton<IPortfolioContextFactory, PortfolioContextFactory>();
        services.AddSingleton<IUpdateContextFactory, UpdateContextFactory>();

        // Register the DbContext with a dummy connection string to satisfy the DI requirements
        services.AddDbContext<PortfolioContext>(options =>
        {
            options.UseSqlite("Data Source=:memory:"); // Dummy connection string
        });

        services.AddDbContext<UpdateContext>(options =>
        {
            options.UseSqlite("Data Source=:memory:"); // Dummy connection string
        });


        //services.AddDbContext<PortfolioContext>(options =>
        //{
        //    options.UseSqlite("Data Source=|DataDirectory|" + Current.DbName);
        //});

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IPriceUpdateService, PriceUpdateService>();
        services.AddScoped<IGraphUpdateService, GraphUpdateService>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddScoped<IPriceLevelService, PriceLevelService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INarrativeService, NarrativeService>();

        services.AddSingleton<IPreferencesService, PreferencesService>();

        services.AddScoped<PortfolioService>();
        services.AddSingleton<IMessenger, WeakReferenceMessenger>();

        return services.BuildServiceProvider();
    }

    private static void InitializeLogger()
    {
#if DEBUG
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Debug(outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(App.AppDataPath + "\\log.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 3,
                    outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
            //.MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();
#else

        Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(App.AppDataPath + "\\log.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 3,
                        outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
#endif
        PreferencesService.AttachLogger();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(App).Name.PadRight(22));
        Logger.Information("------------------------------------");
        Logger.Information("Started Crypto Portfolio Tracker {0}", App.ProductVersion);
        
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception details (optional)
        Logger?.Error($"Unhandled exception: {e.Message}");
        Logger?.Error(e.Exception.ToString());

        // Prevent the application from crashing
        e.Handled = true;

        // Show a user-friendly message
        ShowErrorMessage(e.Message);
    }

    public static async Task ShowErrorMessage(string message)
    {
        Window? tempWindow= null;
        var xamlRoot = MainPage.Current?.XamlRoot;
        if ( xamlRoot is null && Splash is not null)
        {
            xamlRoot= Splash.Content.XamlRoot;
        }
        else 
        {
            tempWindow = new SplashScreen();
            tempWindow.Activate();
            await Task.Delay(1000);
            xamlRoot = tempWindow?.Content.XamlRoot;
        }
        // Create a ContentDialog for the message box
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();

        // Close the temporary window
        tempWindow?.Close();
    }

    public static async Task<ContentDialogResult> ShowContentDialogAsync(ContentDialog dialog)
    {
        dialogCompletionSource.TrySetResult(false); // Reset the completion source
        dialog.Opened += (s, e) => dialogCompletionSource = new TaskCompletionSource<bool>();
        dialog.Closed += (s, e) => dialogCompletionSource.TrySetResult(true);

        return await dialog.ShowAsync();
    }

}
