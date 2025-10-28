using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Converters;
using CryptoPortfolioTracker.Reporting.Services;
using Microsoft.EntityFrameworkCore;

using System.Reflection;
using Task = System.Threading.Tasks.Task;

namespace CryptoPortfolioTracker;

public partial class App : Application
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private Mutex _mutex;
    private const string MutexName = "MyUniqueWinUIMutex";
    private static ILogger? Logger;
    public static readonly SemaphoreSlim UpdateSemaphore = new SemaphoreSlim(1, 1);
    private static Settings _appSettings { get; set; }

    public static App Current { get; private set; }
    public static MainWindow? Window { get; private set; }
    public static Window? Splash { get; set; }
    public static ILocalizer? Localizer { get; private set; }
    public static IServiceProvider Container { get; private set; }

    private static TaskCompletionSource<bool>? dialogCompletionSource; // = new TaskCompletionSource<bool>();
    public static Task DialogCompletionTask => dialogCompletionSource?.Task ?? Task.CompletedTask;
    private static readonly byte[] keyBytes = { 77, 121, 83, 117, 112, 101, 114, 83, 101, 99, 114, 101, 116, 75, 101, 121, 49, 50, 51 };
    private const string TriggerTime = "02:00"; // 2 AM daily
   

    public static bool IsDuressMode { get; set; } = false;

    public App()
    {
        Current = this;
        InitializeComponent();
        this.UnhandledException += OnUnhandledException;

        AppConstants.GetAppEnvironmentals();
        Container = RegisterServices();
        //expose Functions and IValueConverter(s) to XAML too:
       // Application.Current.Resources["FormatValueConverter"] = Container.GetRequiredService<FormatValueToString>();
       // Application.Current.Resources["Functions"] = Container.GetRequiredService<IFunctions>();
        
        _appSettings = Container.GetRequiredService<Settings>();
    }

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Splash = new SplashScreen();
        Splash.Activate();
        await Task.Delay(1000);

        if (!await EnsureSingleInstanceAsync()) return;
        InitializeLogger();
        await InitializeLocalizer();


        MoveUserPreferencesToSettingsIfNeeded();





        var authService = Container.GetRequiredService<AuthenticationService>();
        //******* var code = await authService.GenerateResetCodeAsync(_settings.UserId); //***** TESTING PURPOSES ONLY *****
        bool authenticated = await authService.AuthenticateUserAsync(Splash);
        if (!authenticated)
        {
            Application.Current.Exit();
            return;
        }

        var scheduledTaskService = new ScheduledTaskService(
            AppConstants.ScheduledTaskName,
            AppConstants.ScheduledTaskExe,
            TriggerTime,
            "Daily price update for the Market Charts",
            key => Localizer?.GetLocalizedString(key) ?? key
        );
        await scheduledTaskService.SetupScheduledTaskAsync(Splash);

        await Container.GetService<PortfolioService>().InitializeAsync();

        var iconCacheService = new IconCacheService(AppConstants.IconsPath, Container.GetService<PortfolioService>(), Logger);
        await iconCacheService.CacheLibraryIconsAsync();

        Window = Container.GetService<MainWindow>();
        Window?.Activate();
    }

    private async Task MoveUserPreferencesToSettingsIfNeeded()
    {
        if (File.Exists(AppConstants.AppDataPath + "\\prefs.xml"))
        {
            var prefService = Container.GetService<PreferencesService>();
            if (prefService != null)
            {
                prefService.LoadUserPreferencesFromXml();
                await prefService.AssignUserPreferencesToSettingsAsync();
            }
        }
    }

    private async Task<bool> EnsureSingleInstanceAsync()
    {
        try
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
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to check for single instance: {ex.Message}",
                CloseButtonText = "OK"
            };

            await dialog.ShowAsync();
            return false;
        }
    }

    private async Task InitializeLocalizer()
    {
        var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
            .Build();

        var culture = _appSettings.AppCultureLanguage;
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
        services.AddScoped<IPriceLevelService, PriceLevelService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INarrativeService, NarrativeService>();
        services.AddSingleton<IPreferenceStore, FilePreferenceStore>();

        services.AddSingleton<PreferencesService>();
        services.AddSingleton<Settings>();
        services.AddSingleton<IIndicatorService, IndicatorService>();
        services.AddSingleton<FormatValueToString>(); // converter instance

        services.AddScoped<PortfolioService>();
        services.AddSingleton<IMessenger, WeakReferenceMessenger>();
        services.AddSingleton<IQuestPdfService, QuestPdfService>();

        services.AddSingleton<AuthenticationService>(sp => new AuthenticationService(keyBytes, sp.GetRequiredService<Settings>()));

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
            .WriteTo.File(AppConstants.AppDataPath + "\\log.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 3,
                    outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
            //.MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
            .CreateLogger();
#else

        Log.Logger = new LoggerConfiguration()
                    .WriteTo.File(AppConstants.AppDataPath + "\\log.txt",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 3,
                        outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
#endif
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(App).Name.PadRight(22));
        Logger.Information("------------------------------------");
        Logger.Information("Started Crypto Portfolio Tracker {0}", AppConstants.ProductVersion);
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
            RequestedTheme = _appSettings.AppTheme
        };

        var result = await ShowContentDialogAsync(dialog);
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
