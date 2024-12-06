using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Serilog.Core;
using WinUI3Localizer;

namespace CryptoPortfolioTracker;

public partial class App : Application
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public static Window? Window;
    public static Window? Splash;
    public const string CoinGeckoApiKey = "";
    public static string ApiPath = "https://api.coingecko.com/api/v3/";
    public static string appPath = string.Empty;
    public static string appDataPath = string.Empty;
    public static string ProductVersion = string.Empty;
   // public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
    public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version_onedrive.txt";
    public const string Url = "https://marcel-osft.github.io/CryptoPortfolioTracker/";
    public static bool isBusy;
    public static bool isAppInitializing;

    public static bool isLogWindowEnabled;
    private static ILogger? Logger;
    public static ILocalizer? Localizer;
    public static IPreferencesService _preferencesService;
    public static IServiceProvider Container  { get; private set;  }
    //public Graph PortfolioGraph;

    public static bool initDone;

    public const string DbName = "sqlCPT_Dev.db";


#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public App()
    {
        InitializeComponent();
       
        GetAppEnvironmentals();

        Container = RegisterServices();
        _preferencesService = Container.GetService<IPreferencesService>();
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

        _preferencesService.LoadUserPreferencesFromXml();
       //await LoadPortfolioValueGraph();
        InitializeLogger();
        await InitializeLocalizer();
        CheckDatabase();

        Window = Container.GetService<MainWindow>(); 
        Window.Activate();
    }
    private async Task InitializeLocalizer()
    {
        // Initialize a "Strings" folder in the executables folder.
        var StringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(StringsFolderPath)
            .Build();

         Logger.Information("Setting Language to {0}", _preferencesService.GetAppCultureLanguage());
        await Localizer.SetLanguage(_preferencesService.GetAppCultureLanguage());
    }

    private async void CheckDatabase()
    {
        try
        {
            var context = App.Container.GetService<PortfolioContext>();
            var dbFilename = appDataPath + "\\" + DbName;
            
            if (File.Exists(dbFilename))
            {
                initDone = true;
                var backupFiles = Directory.GetFiles(appDataPath, DbName + "_backup_*");
                if (backupFiles.Length > 4)
                {
                    File.Delete(backupFiles[0]);
                }
                File.Copy(dbFilename, appDataPath + "\\" + DbName + "_backup_"  + DateTime.Now.Ticks.ToString() + ".db");
            }
            
            var pending = await context.Database.GetPendingMigrationsAsync();
            var initPriceLevelsEntity = false;
            foreach (var migration in pending )
            {
                Logger.Information("Pending Migrations {0}", migration.ToString());
                if (initPriceLevelsEntity != true) initPriceLevelsEntity = migration.Contains("AddPriceLevelsEntity") ;
            }

            context?.Database.Migrate();

            var applied = await context.Database.GetAppliedMigrationsAsync();
            foreach (var migration in applied)
            {
                Logger.Information("Applied Migrations {0}", migration.ToString());
            }
            if (initPriceLevelsEntity)
            {
                PopulatePriceLevelsTable(context);
            }

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async void PopulatePriceLevelsTable(PortfolioContext context)
    {
        foreach(Coin coin in context.Coins)
        {
            var pLevelTp = new PriceLevel();
            pLevelTp.Coin = coin;
            pLevelTp.Type = PriceLevelType.TakeProfit;
            pLevelTp.Value = 0;
            pLevelTp.Status = PriceLevelStatus.NotWithinRange;
            pLevelTp.Note = string.Empty;
            context.PriceLevels.Add(pLevelTp);
            
            var pLevelBuy = new PriceLevel();
            pLevelBuy.Coin = coin;
            pLevelBuy.Type = PriceLevelType.Buy;
            pLevelBuy.Value = 0;
            pLevelBuy.Status = PriceLevelStatus.NotWithinRange;
            pLevelBuy.Note = string.Empty;
            context.PriceLevels.Add(pLevelBuy);
            
            var pLevelStop = new PriceLevel();
            pLevelStop.Coin = coin;
            pLevelStop.Type = PriceLevelType.Stop;
            pLevelStop.Value = 0;
            pLevelStop.Status = PriceLevelStatus.NotWithinRange;
            pLevelStop.Note = string.Empty;
            context.PriceLevels.Add(pLevelStop);

        }
        await context.SaveChangesAsync();
    }

    private void GetAppEnvironmentals()
    {
        appPath = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? string.Empty;
        appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CryptoPortfolioTracker";
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }

        AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        ProductVersion = version is not null ? version.ToString() : string.Empty ;
    }
    private IServiceProvider RegisterServices()
    {
        var services = new ServiceCollection();

        services.AddScoped<AssetsView>();
        services.AddScoped<AccountsView>();
        services.AddScoped<CoinLibraryView>();
        services.AddScoped<SettingsView>();
        services.AddScoped<GraphicView>();
        services.AddScoped<MainPage>();
        services.AddScoped<LogWindow>();
        services.AddScoped<MainWindow>();
        services.AddScoped<DashboardView>();
        services.AddScoped<PriceLevelsView>();


        services.AddScoped<AssetsViewModel>();
        services.AddScoped<AccountsViewModel>();
        services.AddScoped<CoinLibraryViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<GraphicViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<PriceLevelsViewModel>();
        services.AddScoped<BaseViewModel>();

        services.AddDbContext<PortfolioContext>(options =>
        {
            options.UseSqlite("Data Source=|DataDirectory|" + DbName);
        });

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IPriceUpdateService, PriceUpdateService>();
        services.AddScoped<IGraphUpdateService, GraphUpdateService>();
        services.AddSingleton<IGraphService, GraphService>();
        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddScoped<IPriceLevelService, PriceLevelService>();


        return services.BuildServiceProvider();
    }

    private void InitializeLogger()
    {
        var commandLineArgs = Environment.GetCommandLineArgs();

        if (commandLineArgs.Length > 1 && commandLineArgs[1][1..].ToLower() == "log")
        {
            isLogWindowEnabled = true; // the Logger window is created in MainPage...
        }
        else
        {
#if DEBUG
            Log.Logger = new LoggerConfiguration()
               .WriteTo.Debug(outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
               .WriteTo.File(App.appDataPath + "\\log.txt",
                       rollingInterval: RollingInterval.Day,
                       retainedFileCountLimit: 3,
                       outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
               //.MinimumLevel.Is(Serilog.Events.LogEventLevel.Verbose)
               .CreateLogger();
#else

            Log.Logger = new LoggerConfiguration()
                        .WriteTo.File(App.appDataPath + "\\log.txt",
                           rollingInterval: RollingInterval.Day,
                           retainedFileCountLimit: 3,
                           outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
                  .CreateLogger();
#endif
            _preferencesService.AttachLogger();
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(App).Name.PadRight(22));
            Logger.Information("------------------------------------");
            Logger.Information("Started Crypto Portfolio Tracker {0}", App.ProductVersion);
        }
    }


}
