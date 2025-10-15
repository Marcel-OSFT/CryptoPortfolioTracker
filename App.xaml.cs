using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Initializers;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WinUI3Localizer;
using Task = System.Threading.Tasks.Task;

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

    private static TaskCompletionSource<bool>? dialogCompletionSource; // = new TaskCompletionSource<bool>();
    public static Task DialogCompletionTask => dialogCompletionSource?.Task ?? Task.CompletedTask;
    private static string AuthStateFile => Path.Combine(AppDataPath, "authstate.json");
    private static readonly byte[] keyBytes = { 77, 121, 83, 117, 112, 101, 114, 83, 101, 99, 114, 101, 116, 75, 101, 121, 49, 50, 51 };

    //public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version_onedrive.txt";
    public const string Url = "https://marcel-osft.github.io/CryptoPortfolioTracker/";
    public const string CoinGeckoApiKey = "";
    public const string ApiPath = "https://api.coingecko.com/api/v3/";
    public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
    public const string DefaultPortfolioGuid = "f52ee1a8-ea8d-4f21-849c-6e6429f88256";
    public const string DefaultDuressPortfolioGuid = "08c1ac97-27e0-4922-93da-320c8a5e08ba";

    public const string DbName = "sqlCPT.db";
    public const string PrefFileName = "prefs.xml";
    public const string BackupFolder = "Backup";
    public const string PrefixBackupName = "RestorePoint";
    public const string ExtentionBackup = "cpt";
    public const string IconsFolder = "LibraryIcons";
    public const string PortfoliosFileName = "portfolios.json";
    public static string PortfoliosPath { get; private set; }
    public static string ChartsFolder { get; private set; }
    public static string ScheduledTaskExe { get; private set; }
    public static string PowerShellScriptPs1 { get; private set; }

    private const string TriggerTime = "02:00"; // 2 AM daily
    public const string ScheduledTaskName = "CryptoPortfolioTracker MarketCharts Update Task";

    public static bool IsDuressMode { get; set; } = false;

    public App()
    {
        Current = this;
        InitializeComponent();
        this.UnhandledException += OnUnhandledException;

        GetAppEnvironmentals();
        Container = RegisterServices();
        PreferencesService = Container.GetService<IPreferencesService>() ?? throw new InvalidOperationException("Failed to retrieve IPreferencesService from the service container.");

    }

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Splash = new SplashScreen();
        Splash.Activate();
        await Task.Delay(1000);

        if (!await EnsureSingleInstanceAsync()) return;
        PreferencesService.LoadUserPreferencesFromXml();
        InitializeLogger();
        await InitializeLocalizer();

        var authService = new AuthenticationService(PreferencesService, keyBytes);
        // ******* var code = await authService.GenerateResetCodeAsync("4fc3cd2e-4114-4683-88ab-bd7c57427649");
        bool authenticated = await authService.AuthenticateUserAsync(Splash);
        if (!authenticated)
        {
            Application.Current.Exit();
            return;
        }

        
        var scheduledTaskService = new ScheduledTaskService(
            ScheduledTaskName, 
            ScheduledTaskExe, 
            TriggerTime, 
            "Daily price update for the Market Charts", 
            key => Localizer?.GetLocalizedString(key) ?? key
        );
        await scheduledTaskService.SetupScheduledTaskAsync(Splash);

        await App.Container.GetService<PortfolioService>().InitializeAsync();

        var iconCacheService = new IconCacheService(App.IconsFolder, App.Container.GetService<PortfolioService>(), Logger);
        await iconCacheService.CacheLibraryIconsAsync();

        Window = Container.GetService<MainWindow>();
        Window?.Activate();

    }

    private async Task<bool> EnsureSingleInstanceAsync()
    {
        _mutex = new Mutex(false, MutexName, out bool createdNew);

        if (!createdNew && !AdminCheck.IsRunAsAdmin())
        {
            await ShowErrorMessage("Another instance of the application is already running.");
            _mutex.Close();
            _mutex = null;
            Application.Current.Exit();
            return false;
        }
        return true;
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
        ProductVersion = version is not null ? version.ToString() : string.Empty;

        PortfoliosPath = Path.Combine(AppDataPath, "Portfolios");
        ChartsFolder = Path.Combine(AppDataPath, "MarketCharts");
        PowerShellScriptPs1 = Path.Combine(AppPath, "RegisterScheduledTask.ps1");

        //ScheduledTaskExe = Path.Combine(AppPath, "MarketChartsUpdateService.exe");

        if (Debugger.IsAttached)
        {
            // Development mode (running from IDE)
            ScheduledTaskExe = "C:\\Program Files\\Crypto Portfolio Tracker\\MarketChartsUpdateService.exe";
        }
        else
        {
            // Production mode
            ScheduledTaskExe = Path.Combine(AppPath, "MarketChartsUpdateService.exe");
        }
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
        //    options.UseSqlite("Data Source=|DataDirectory|" + DbName);
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
            .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Debug)
                .Enrich.FromLogContext()
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

    public void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception details (optional)
        Logger?.Error($"Unhandled exception: {e.Message}");
        Logger?.Error(e.Exception.ToString());

        // Prevent the application from crashing
        e.Handled = true;

        // Show a user-friendly message
        _ = ShowErrorMessage(e.Message);
    }

    public static async Task ShowErrorMessage(string message)
    {
        Window? tempWindow = null;
        var xamlRoot = MainPage.Current?.XamlRoot;
        if (xamlRoot == null && Splash is not null)
        {
            xamlRoot = Splash.Content.XamlRoot;
        }
        else if (xamlRoot == null)
        {
            tempWindow = new SplashScreen();
            tempWindow.Activate();
            await Task.Delay(1000);
            xamlRoot = tempWindow?.Content.XamlRoot;
        }
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();

        tempWindow?.Close();
    }

    public static async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
    {
        Window? tempWindow = null;
        var xamlRoot = MainPage.Current?.XamlRoot;
        if (xamlRoot == null && Splash is not null)
        {
            xamlRoot = Splash.Content.XamlRoot;
        }
        else if (xamlRoot == null)
        {
            tempWindow = new SplashScreen();
            tempWindow.Activate();
            await Task.Delay(1000);
            xamlRoot = tempWindow?.Content.XamlRoot;
        }
        var dialog = new ContentDialog()
        {
            Title = title,
            XamlRoot = xamlRoot,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            RequestedTheme = PreferencesService.GetAppTheme(),
        };

        var result = await App.ShowContentDialogAsync(dialog);
        return result;
    }

    public static async Task<ContentDialogResult> ShowContentDialogAsync(ContentDialog dialog)
    {
        dialogCompletionSource = new TaskCompletionSource<bool>();

        dialog.Closed += (s, e) => dialogCompletionSource.TrySetResult(true);

        var result = await dialog.ShowAsync();

        // Ensure the completion source is set in case Closed wasn't triggered
        if (!dialogCompletionSource.Task.IsCompleted)
            dialogCompletionSource.TrySetResult(true);

        return result;
    }

}
