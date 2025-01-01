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
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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

        AddNewTeachingTips();


       //await LoadPortfolioValueGraph();
        InitializeLogger();
        await InitializeLocalizer();
        await CheckDatabase();

        Window = Container.GetService<MainWindow>(); 
        Window.Activate();
    }

    private void AddNewTeachingTips()
    {
        var newTip1 = new TeachingTipCPT(){ Name = "TeachingTipNarrLibr", IsShown = false };
        var newTip2 = new TeachingTipCPT(){ Name = "TeachingTipNarrDash", IsShown = false };
        var newTip3 = new TeachingTipCPT(){ Name = "TeachingTipNarrNarr", IsShown = false };
        var newTip4 = new TeachingTipCPT(){ Name = "TeachingTipPortDash", IsShown = false };
        var newTip5 = new TeachingTipCPT(){ Name = "TeachingTipNarrNavi", IsShown = false };

        List<TeachingTipCPT> tips = new() { newTip1, newTip2, newTip3, newTip4, newTip5};

        _preferencesService.AddTeachingTips(tips);

    }

    private async Task InitializeLocalizer()
    {
        // Initialize a "Strings" folder in the executables folder.
        var StringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(StringsFolderPath)
            .Build();

        if (Logger != null) { Logger.Information("Setting Language to {0}", _preferencesService.GetAppCultureLanguage()); }
        await Localizer.SetLanguage(_preferencesService.GetAppCultureLanguage());
    }

    private async Task CheckDatabase()
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
            var initNarrativesEntity = false;

            if (pending.Count() > 0)
            {
                foreach (var migration in pending)
                {
                    if (Logger != null) { Logger.Information("Pending Migrations {0}", migration.ToString()); }
                    if (initPriceLevelsEntity != true) initPriceLevelsEntity = migration.Contains("AddPriceLevelsEntity");
                    if (initNarrativesEntity != true) initNarrativesEntity = migration.Contains("AddNarrativesEntity");
                }

                if (File.Exists(dbFilename)) BackupCptFiles(true);
            }

            context?.Database.Migrate();

            if (initPriceLevelsEntity) { PopulatePriceLevelsTable(context); }
            if (initNarrativesEntity) 
            { 
                PopulateNarrativesTable(context); 
            } 

            var applied = await context.Database.GetAppliedMigrationsAsync();
            foreach (var migration in applied)
            {
                if (Logger != null) { Logger.Information("Applied Migrations {0}", migration.ToString()); }
            }
        }
        catch (Exception ex)
        {
            if (Logger != null) { Logger.Error(ex.Message, "Checking Database failed!"); }
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

        try
        {
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
        catch (Exception ex)
        {
            if (Logger != null) { Logger.Error(ex.Message, "BackUp CPT files failed!"); }
        }
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
            levels.Add(pLevelTp);

            var pLevelBuy = new PriceLevel();
            pLevelBuy.Coin = coin;
            pLevelBuy.Type = PriceLevelType.Buy;
            pLevelBuy.Value = 0;
            pLevelBuy.Status = PriceLevelStatus.NotWithinRange;
            pLevelBuy.Note = string.Empty;
            levels.Add(pLevelBuy);

            var pLevelStop = new PriceLevel();
            pLevelStop.Coin = coin;
            pLevelStop.Type = PriceLevelType.Stop;
            pLevelStop.Value = 0;
            pLevelStop.Status = PriceLevelStatus.NotWithinRange;
            pLevelStop.Note = string.Empty;
            levels.Add(pLevelStop);

            context.PriceLevels.AddRange(levels);

        }
        await context.SaveChangesAsync();
    }

    private async void PopulateNarrativesTable(PortfolioContext context)
    {
        if (context.Narratives.Count() != 0) return;

        // Create a list of Narrative items with descriptions
        var narratives = new List<Narrative>
        {
            new Narrative { Id = 1, Name = "- Not Assigned -", About = "Default setting in case you don't want to assign narratives" },
            new Narrative { Id = 2, Name = "AI", About = "AI in crypto refers to the use of artificial intelligence to optimize trading, provide market insights, and enhance security." },
            new Narrative { Id = 3, Name = "Appchain", About = "Appchains are application-specific blockchains designed to optimize performance for particular decentralized applications (DApps)." },
            new Narrative { Id = 4,  Name = "DeFI", About = "DeFi (Decentralized Finance) aims to recreate traditional financial systems using decentralized technologies like blockchain." },
            new Narrative { Id = 5, Name = "DEX", About = "DEX (Decentralized Exchange) allows users to trade cryptocurrencies directly without an intermediary, leveraging smart contracts." },
            new Narrative { Id = 6, Name = "DePin", About = "DePin (Decentralized Physical Infrastructure Networks) combines blockchain with physical infrastructures like IoT to create decentralized networks." },
            new Narrative { Id = 7, Name = "Domains", About = "Blockchain domains offer decentralized, censorship-resistant alternatives to traditional domain names, enhancing ownership and control." },
            new Narrative { Id = 8, Name = "Gamble-Fi", About = "Gamble-Fi integrates decentralized finance principles with online gambling, providing transparent and secure gaming experiences." },
            new Narrative { Id = 9, Name = "Game-Fi", About = "Game-Fi combines gaming and decentralized finance, allowing players to earn cryptocurrency and trade in-game assets." },
            new Narrative { Id = 10, Name = "Social-Fi", About = "Social-Fi integrates social media with decentralized finance, enabling monetization and decentralized governance of social platforms." },
            new Narrative { Id = 11, Name = "Interoperability", About = "Interoperability focuses on enabling different blockchain networks to communicate and interact, facilitating seamless asset transfers and data exchange." },
            new Narrative { Id = 12, Name = "Layer 1s", About = "Layer 1s are the base layer blockchains like Bitcoin and Ethereum that provide the foundational security and consensus mechanisms." },
            new Narrative { Id = 13, Name = "Layer 2s", About = "Layer 2s are scaling solutions built on top of Layer 1 blockchains to improve transaction speed and reduce fees." },
            new Narrative { Id = 14, Name = "LSD", About = "LSD (Liquid Staking Derivatives) allow users to stake assets and receive liquid tokens that can be used in DeFi activities." },
            new Narrative { Id = 15, Name = "Meme", About = "Meme coins are cryptocurrencies inspired by internet memes, often characterized by high volatility and community-driven value." },
            new Narrative { Id = 16, Name = "NFT", About = "NFTs (Non-Fungible Tokens) are unique digital assets representing ownership of items like art, music, and virtual real estate." },
            new Narrative { Id = 17, Name = "Privacy", About = "Privacy coins and technologies aim to enhance transaction anonymity and data protection on the blockchain." },
            new Narrative { Id = 18, Name = "Real Yield", About = "Real Yield focuses on generating sustainable returns through staking, lending, and other DeFi activities with real-world asset backing." },
            new Narrative { Id = 19, Name = "RWA", About = "RWA (Real World Assets) are physical assets like real estate or commodities tokenized on the blockchain for easier trading and investment." },
            new Narrative { Id = 20, Name = "CEX", About = "CEX (Centralized Exchange) refers to traditional cryptocurrency exchanges where trades are managed by a central entity." },
            new Narrative { Id = 21, Name = "Stablecoins", About = "Stablecoins are cryptocurrencies pegged to stable assets like fiat currencies to minimize price volatility." },
            new Narrative { Id = 22, Name = "Others", About = "Narrative for coins that you don't want to assign a specific Narrative" }
        };

        context.Narratives.AddRange(narratives);

        await context.SaveChangesAsync();

        if (context.Coins is not null && context.Coins.Count() > 0)
        {
            var coins = await context.Coins.ToListAsync();
            var initialNarrative = context.Narratives.Where(x => x.Id == 1).First();
            foreach (var coin in coins)
            {
               coin.Narrative = initialNarrative;
            }
            context.Coins.UpdateRange(coins);
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
        services.AddScoped<NarrativesView>();


        services.AddScoped<AssetsViewModel>();
        services.AddScoped<AccountsViewModel>();
        services.AddScoped<CoinLibraryViewModel>();
        services.AddScoped<SettingsViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<PriceLevelsViewModel>();
        services.AddScoped<BaseViewModel>();
        services.AddScoped<NarrativesViewModel>();

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
        services.AddScoped<INarrativeService, NarrativeService>();


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
