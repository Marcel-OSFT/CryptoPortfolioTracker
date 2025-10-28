using System.Reflection;

namespace CryptoPortfolioTracker.Configuration;

public static class AppConstants
{
    // Static constants
    public const string Url = "https://marcel-osft.github.io/CryptoPortfolioTracker/";
    public const string CoinGeckoApiKey = "";
    public const string ApiPath = "https://api.coingecko.com/api/v3/";
    public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
    public const string DefaultPortfolioGuid = "f52ee1a8-ea8d-4f21-849c-6e6429f88256";
    public const string DefaultDuressPortfolioGuid = "08c1ac97-27e0-4922-93da-320c8a5e08ba";
    public const string ScheduledTaskName = "CryptoPortfolioTracker MarketCharts Update Task";
    public const string DbName = "sqlCPT.db";
    public const string PrefFileName = "prefs.xml";
    public const string BackupFolder = "Backup";
    public const string PrefixBackupName = "RestorePoint";
    public const string ExtentionBackup = "cpt";
    public const string PortfoliosFileName = "portfolios.json";

    // Runtime-initialized paths (set from startup code / GetAppEnvironmentals)
    public static string AppPath { get; set; } = string.Empty;
    public static string AppDataPath { get; set; } = string.Empty;
    public static string ProductVersion { get; set; } = string.Empty;
    public static string PortfoliosPath { get; set; } = string.Empty;
    public static string IconsPath { get; set; } = string.Empty;
    public static string ChartsFolder { get; set; } = string.Empty;
    public static string ScheduledTaskExe { get; set; } = string.Empty;
    public static string PowerShellScriptPs1 { get; set; } = string.Empty;
    public static string AuthStateFile { get; set; } = string.Empty;


    public static void GetAppEnvironmentals()
    {
        AppConstants.AppPath = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory) ?? string.Empty;
        AppConstants.AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CryptoPortfolioTracker";
        if (!Directory.Exists(AppConstants.AppDataPath))
        {
            Directory.CreateDirectory(AppConstants.AppDataPath);
        }

        AppDomain.CurrentDomain.SetData("DataDirectory", AppConstants.AppDataPath);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        AppConstants.ProductVersion = version is not null ? version.ToString() : string.Empty;

        AppConstants.PortfoliosPath = Path.Combine(AppConstants.AppDataPath, "Portfolios");
        AppConstants.ChartsFolder = Path.Combine(AppConstants.AppDataPath, "MarketCharts");
        AppConstants.PowerShellScriptPs1 = Path.Combine(AppConstants.AppPath, "RegisterScheduledTask.ps1");
        AppConstants.IconsPath = Path.Combine(AppConstants.AppDataPath, "LibraryIcons");
        AppConstants.AuthStateFile = Path.Combine(AppConstants.AppDataPath, "authstate.json");

        if (Debugger.IsAttached)
        {
            // Development mode (running from IDE)
            AppConstants.ScheduledTaskExe = "C:\\Program Files\\Crypto Portfolio Tracker\\MarketChartsUpdateService.exe";
        }
        else
        {
            // Production mode
            AppConstants.ScheduledTaskExe = Path.Combine(AppConstants.AppPath, "MarketChartsUpdateService.exe");
        }
    }

}