﻿using System;
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
using LanguageExt;
using System.Net.Http;

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
    public bool needFixFaultyMigration = false;

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
    public const string IconsFolder = "LibraryIcons";


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
        
        InitializeLogger();
        await InitializeLocalizer();
        await CheckDatabase();
        CacheLibraryIcons();
        Window = Container.GetService<MainWindow>(); 
        Window.Activate();
    }

    private async Task CacheLibraryIcons()
    {
        var iconsFolderPath = Path.Combine(appDataPath, IconsFolder);
        if (Directory.Exists(iconsFolderPath))
        {
            Directory.GetFiles(iconsFolderPath).ToList().ForEach(File.Delete);
        }
        else
        {
            Directory.CreateDirectory(iconsFolderPath);
        }

        var context = Container.GetService<PortfolioContext>();
        var coins = context.Coins.Where(coin => !string.IsNullOrEmpty(coin.ImageUri)).ToList();

        foreach (var coin in coins)
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
        }
    }

    private static async Task<bool> RetrieveCoinIconAsync(Coin? coin, string iconPath)
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = await httpClient.GetAsync(coin.ImageUri);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using var fs = new FileStream(iconPath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private string ExtractFileNameFromUri(string uri)
    {
        var uriWithoutQuery = uri.Split('?')[0];
        return Path.GetFileName(uriWithoutQuery);
    }

    private void AddNewTeachingTips()
    {
        var tips = new List<TeachingTipCPT>
        {
            new() { Name = "TeachingTipNarrLibr", IsShown = false },
            new() { Name = "TeachingTipNarrDash", IsShown = false },
            new() { Name = "TeachingTipNarrNarr", IsShown = false },
            new() { Name = "TeachingTipPortDash", IsShown = false },
            new() { Name = "TeachingTipNarrNavi", IsShown = false }
        };

        _preferencesService.AddTeachingTipsIfNotExist(tips);
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
            var dbFilename = Path.Combine(appDataPath, DbName);

            if (File.Exists(dbFilename))
            {
                BackupCptFiles(false);
            }

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var initPriceLevelsEntity = pendingMigrations.Any(m => m.Contains("AddPriceLevelsEntity"));
            var initNarrativesEntity = pendingMigrations.Any(m => m.Contains("AddNarrativesEntity"));

            if (pendingMigrations.Any())
            {
                foreach (var migration in pendingMigrations)
                {
                    Logger?.Information("Pending Migrations {0}", migration);
                }

                if (File.Exists(dbFilename)) BackupCptFiles(true);
            }

            //var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync());
            needFixFaultyMigration = (await context.Database.GetAppliedMigrationsAsync()).Contains("20241228225250_AddNarrativesEntity");

            await context.Database.MigrateAsync();

            if (initPriceLevelsEntity)
            {
                SeedPriceLevelsTable(context);
            }
            if (initNarrativesEntity)
            {
                SeedNarrativesTable(context);
            }

            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            foreach (var migration in appliedMigrations)
            {
                Logger?.Information("Applied Migrations {0}", migration);
            }
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "Checking Database failed!");
        }
    }


    private void BackupCptFiles(bool isMigration)
    {
        var dbFile = Path.Combine(appDataPath, DbName);
        var preferencesFile = Path.Combine(appDataPath, PrefFileName);
        var chartsFolder = Path.Combine(appDataPath, ChartsFolder);
        var tempFolder = Path.Combine(appDataPath, "Temp");
        var backupFolder = Path.Combine(appDataPath, BackupFolder);
        string backUpName;

        try
        {
            Directory.CreateDirectory(backupFolder);

            if (isMigration)
            {
                if (Directory.GetFiles(backupFolder, "*" + ProductVersion.Replace(".", "-") + "*").Any())
                {
                    return;
                }
                backUpName = $"{PrefixBackupName}_m{ProductVersion.Replace(".", "-")}_{DateTime.Now:yyyyMMdd-HHmmss}.{ExtentionBackup}";
            }
            else
            {
                initDone = true;
                var backupFiles = Directory.GetFiles(backupFolder, "*_s_*");
                if (backupFiles.Length > 5)
                {
                    File.Delete(backupFiles[0]);
                }
                backUpName = $"{PrefixBackupName}_s_{DateTime.Now:yyyyMMdd-HHmmss}.{ExtentionBackup}";
            }

            Directory.CreateDirectory(tempFolder);
            File.Copy(dbFile, Path.Combine(tempFolder, DbName));
            File.Copy(preferencesFile, Path.Combine(tempFolder, PrefFileName));

            Directory.CreateDirectory(chartsFolder);
            DirectoryCopy(chartsFolder, Path.Combine(tempFolder, ChartsFolder), true);
            ZipFile.CreateFromDirectory(tempFolder, Path.Combine(backupFolder, backUpName));
            Directory.Delete(tempFolder, true);
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, "BackUp CPT files failed!");
        }
    }

    private async void SeedPriceLevelsTable(PortfolioContext context)
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

    private async void SeedNarrativesTable(PortfolioContext context)
    {
        if (context.Narratives.Count() > 1) return;

        // Create a list of Narrative items with descriptions
        var narratives = new List<Narrative>
        {
            //new Narrative { Id = 1, Name = "- Not Assigned -", About = "Default setting in case you don't want to assign narratives" },
            new Narrative { Name = "AI", About = "AI in crypto refers to the use of artificial intelligence to optimize trading, provide market insights, and enhance security." },
            new Narrative { Name = "Appchain", About = "Appchains are application-specific blockchains designed to optimize performance for particular decentralized applications (DApps)." },
            new Narrative { Name = "DeFI", About = "DeFi (Decentralized Finance) aims to recreate traditional financial systems using decentralized technologies like blockchain." },
            new Narrative { Name = "DEX", About = "DEX (Decentralized Exchange) allows users to trade cryptocurrencies directly without an intermediary, leveraging smart contracts." },
            new Narrative { Name = "DePin", About = "DePin (Decentralized Physical Infrastructure Networks) combines blockchain with physical infrastructures like IoT to create decentralized networks." },
            new Narrative { Name = "Domains", About = "Blockchain domains offer decentralized, censorship-resistant alternatives to traditional domain names, enhancing ownership and control." },
            new Narrative { Name = "Gamble-Fi", About = "Gamble-Fi integrates decentralized finance principles with online gambling, providing transparent and secure gaming experiences." },
            new Narrative { Name = "Game-Fi", About = "Game-Fi combines gaming and decentralized finance, allowing players to earn cryptocurrency and trade in-game assets." },
            new Narrative { Name = "Social-Fi", About = "Social-Fi integrates social media with decentralized finance, enabling monetization and decentralized governance of social platforms." },
            new Narrative { Name = "Interoperability", About = "Interoperability focuses on enabling different blockchain networks to communicate and interact, facilitating seamless asset transfers and data exchange." },
            new Narrative { Name = "Layer 1s", About = "Layer 1s are the base layer blockchains like Bitcoin and Ethereum that provide the foundational security and consensus mechanisms." },
            new Narrative { Name = "Layer 2s", About = "Layer 2s are scaling solutions built on top of Layer 1 blockchains to improve transaction speed and reduce fees." },
            new Narrative { Name = "LSD", About = "LSD (Liquid Staking Derivatives) allow users to stake assets and receive liquid tokens that can be used in DeFi activities." },
            new Narrative { Name = "Meme", About = "Meme coins are cryptocurrencies inspired by internet memes, often characterized by high volatility and community-driven value." },
            new Narrative { Name = "NFT", About = "NFTs (Non-Fungible Tokens) are unique digital assets representing ownership of items like art, music, and virtual real estate." },
            new Narrative { Name = "Privacy", About = "Privacy coins and technologies aim to enhance transaction anonymity and data protection on the blockchain." },
            new Narrative { Name = "Real Yield", About = "Real Yield focuses on generating sustainable returns through staking, lending, and other DeFi activities with real-world asset backing." },
            new Narrative { Name = "RWA", About = "RWA (Real World Assets) are physical assets like real estate or commodities tokenized on the blockchain for easier trading and investment." },
            new Narrative { Name = "CEX", About = "CEX (Centralized Exchange) refers to traditional cryptocurrency exchanges where trades are managed by a central entity." },
            new Narrative { Name = "Stablecoins", About = "Stablecoins are cryptocurrencies pegged to stable assets like fiat currencies to minimize price volatility." },
            new Narrative { Name = "Others", About = "Narrative for coins that you don't want to assign a specific Narrative" }
        };

        context.Narratives.AddRange(narratives);

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
