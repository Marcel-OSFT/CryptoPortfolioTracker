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
using WinUI3Localizer;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.Store;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace CryptoPortfolioTracker;

public partial class App : Application
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public static App Current;
    public static MainWindow? Window { get; private set; }
    public static Window? Splash;
    public const string CoinGeckoApiKey = "";
    public static string ApiPath = "https://api.coingecko.com/api/v3/";
    public static string appPath = string.Empty;
    public static string appDataPath = string.Empty;
    public static string ProductVersion = string.Empty;
    public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
    
    //public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version_onedrive.txt";
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

    public const string DbName = "sqlCPT.db";
    public const string PrefFileName = "prefs.xml";
    public const string ChartsFolder = "MarketCharts";
    public const string BackupFolder = "Backup";
    public const string PrefixBackupName = "CPTbackup";
    public const string ExtentionBackup = "cpt";


    //public const string DbRestoreName = "sqlCPT_DevRestore.db";
    //public const string DbName = "sqlCPT.db";


#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public App()
    {
        Current = this;
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
                BackupCptFiles(false);
            }


            var pending = await context.Database.GetPendingMigrationsAsync();
            var initPriceLevelsEntity = false;

            if (pending.Count() > 0)
            {
                foreach (var migration in pending)
                {
                    Logger.Information("Pending Migrations {0}", migration.ToString());
                    if (initPriceLevelsEntity != true) initPriceLevelsEntity = migration.Contains("AddPriceLevelsEntity");
                }

                if (File.Exists(dbFilename)) BackupCptFiles(true);
            }

            context?.Database.Migrate();

            if (initPriceLevelsEntity)
            {
                PopulatePriceLevelsTable(context);
            }


            var applied = await context.Database.GetAppliedMigrationsAsync();
            foreach (var migration in applied)
            {
                Logger.Information("Applied Migrations {0}", migration.ToString());
            }
            

        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private void BackupCptFiles(bool isMigration)
    {
        var dbFile = appDataPath + "\\" + DbName;
        var preferencesFile = appDataPath + "\\" + PrefFileName;
        var chartsFolder = appDataPath + "\\" + ChartsFolder;
        var tempFolder = appDataPath + "\\Temp";
        var backupFolder = appDataPath + "\\" + BackupFolder;
        string backUpName = string.Empty;

        if (!Directory.Exists(backupFolder))
        {
            Directory.CreateDirectory(backupFolder);
        }

        if (isMigration)
        {
            //*** Skip migration backup if backup already exists
            var file = Directory.GetFiles(backupFolder, "*" + ProductVersion.Replace(".", "-") + "*");
            if (file.Length > 0)
            {
                return;
            }
            backUpName = PrefixBackupName + "_m" + ProductVersion.Replace(".", "-") + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "." + ExtentionBackup;
        }
        else
        {
            initDone = true;
            var backupFiles = Directory.GetFiles(backupFolder, "*_s_*");
            if (backupFiles.Length > 5)
            {
                File.Delete(backupFiles[0]);
            }
            backUpName = PrefixBackupName + "_s_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "." + ExtentionBackup;

        }

        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }
        else
        {
            Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);
        }
        File.Copy(dbFile, tempFolder + "\\" + DbName);
        File.Copy(preferencesFile, tempFolder + "\\" + PrefFileName);
        
        if (!Directory.Exists(chartsFolder))
        {
            Directory.CreateDirectory(chartsFolder);
        }

        DirectoryCopy(chartsFolder, tempFolder + "\\" + ChartsFolder, true);

        ZipFile.CreateFromDirectory(tempFolder, backupFolder + "\\" + backUpName);

        Directory.Delete(tempFolder, true);

    }

    


    private async void PopulatePriceLevelsTable(PortfolioContext context)
    {
        if (context.Coins.Count() == 0) return;

        foreach(Coin coin in context.Coins)
        {
            List<PriceLevel> levels = new();

            var pLevelTp = new PriceLevel();
            pLevelTp.Coin = coin;
            pLevelTp.Type = PriceLevelType.TakeProfit;
            pLevelTp.Value = 0;
            pLevelTp.Status = PriceLevelStatus.NotWithinRange;
            pLevelTp.Note = string.Empty;
            //context.PriceLevels.Add(pLevelTp);
            levels.Add(pLevelTp);


            var pLevelBuy = new PriceLevel();
            pLevelBuy.Coin = coin;
            pLevelBuy.Type = PriceLevelType.Buy;
            pLevelBuy.Value = 0;
            pLevelBuy.Status = PriceLevelStatus.NotWithinRange;
            pLevelBuy.Note = string.Empty;
            //context.PriceLevels.Add(pLevelBuy);
            levels.Add(pLevelBuy);



            var pLevelStop = new PriceLevel();
            pLevelStop.Coin = coin;
            pLevelStop.Type = PriceLevelType.Stop;
            pLevelStop.Value = 0;
            pLevelStop.Status = PriceLevelStatus.NotWithinRange;
            pLevelStop.Note = string.Empty;
            //context.PriceLevels.Add(pLevelStop);
            levels.Add(pLevelStop);

            context.PriceLevels.AddRange(levels);
           // coin.PriceLevels = levels;

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
        services.AddScoped<MainPage>();
        services.AddScoped<LogWindow>();
        services.AddScoped<MainWindow>();
        services.AddScoped<DashboardView>();
        services.AddScoped<PriceLevelsView>();


        services.AddScoped<AssetsViewModel>();
        services.AddScoped<AccountsViewModel>();
        services.AddScoped<CoinLibraryViewModel>();
        services.AddScoped<SettingsViewModel>();
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
        services.AddScoped<IDashboardService, DashboardService>();


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
    private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);
        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the source directory does not exist, throw an exception.
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        // If the destination directory does not exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the file contents of the directory to copy.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs);
            }
        }
    }
    


}
