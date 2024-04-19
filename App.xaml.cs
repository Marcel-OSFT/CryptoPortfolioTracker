using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Serilog;
using Serilog.Core;
using Windows.Storage;
using WinUI3Localizer;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker;


/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>

    public static Window Window;
    public static Window Splash;
    public const string CoinGeckoApiKey = "";
    public static string ApiPath = "https://api.coingecko.com/api/v3/";
    public static string appPath;
    public static string appDataPath;
    public static string ProductVersion;
    public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
    public static bool isBusy;
    public static UserPreferences userPreferences;
    public static bool isAppInitializing;

    public static bool isLogWindowEnabled;
    private ILogger Logger;
    public static ILocalizer Localizer;

    public static IServiceProvider Container
    {
        get; private set;
    }
    

    public App()
    {
        this.InitializeComponent();
        GetAppEnvironmentals();

        Container = RegisterServices();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Splash = new SplashScreen();
        Splash.Activate();
        await Task.Delay(1000);

        GetUserPreferences();
        await InitializeLocalizer();
        InitializeLogger();
        CheckDatabase();

        Window = new MainWindow();
        Window.Activate();
    }
    /// <summary>
    /// GetUserPreferences need to be called in the App's Constructor, because of setting the RequestedTheme
    /// which only can be done in the constructor
    /// </summary>
    private void GetUserPreferences()
    {
        //userPreferences = new UserPreferences();
        try
        {
            if (File.Exists(appDataPath + "\\prefs.xml"))
            {
                isAppInitializing = true;
                XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
                FileStream myFileStream = new FileStream(appDataPath + "\\prefs.xml", FileMode.Open);
                userPreferences = (UserPreferences)mySerializer.Deserialize(myFileStream);
            }
        }
        catch { }
        finally
        {
            isAppInitializing = false;
        }
    }

    private async Task InitializeLocalizer()
    {
        // Initialize a "Strings" folder in the executables folder.
        string StringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");
        StorageFolder stringsFolder = await StorageFolder.GetFolderFromPathAsync(StringsFolderPath);

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(StringsFolderPath)
            //.SetOptions(options =>
            //{
            //    options.DefaultLanguage = "nl";
            //})
            .Build();

        await Localizer.SetLanguage(userPreferences.AppCultureLanguage);
    }

    private void CheckDatabase()
    {
        var context = App.Container.GetService<PortfolioContext>();
        context.Database.EnsureCreated();
    }


    private void GetAppEnvironmentals()
    {
        appPath = System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory);
        appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CryptoPortfolioTracker";
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
        AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);
        ProductVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
    private IServiceProvider RegisterServices()
    {
        var services = new ServiceCollection();

        services.AddScoped<AssetsView>();
        services.AddScoped<AccountsView>();
        services.AddScoped<CoinLibraryView>();
        services.AddScoped<HelpView>();
        services.AddScoped<SettingsView>();

        services.AddScoped<AssetsViewModel>();
        services.AddScoped<AccountsViewModel>();
        services.AddScoped<CoinLibraryViewModel>();
        services.AddScoped<HelpViewModel>();
        services.AddScoped<SettingsViewModel>();

        services.AddDbContext<PortfolioContext>(options =>
        {
            options.UseSqlite("Data Source=|DataDirectory|sqlCPT.db");
            //options.UseSqlite("Data Source=C:\\Users\\marce\\AppData\\Local\\CryptoPortfolioTracker\\sqlCPT.db");
        });

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<IPriceUpdateService, PriceUpdateService>(serviceProvider =>
        {
            return new(TimeSpan.FromMinutes(App.userPreferences.RefreshIntervalMinutes));
        });

        return services.BuildServiceProvider();
    }



//    private void ConfigureWindow(Window window)
//    {
//        var monitor = MonitorInfo.GetDisplayMonitors().FirstOrDefault();
//        if (monitor != null && monitor.RectMonitor.Width <= 1024 && monitor.RectMonitor.Height <= 768)
//        {
//            window.SetWindowSize(monitor.RectMonitor.Width, monitor.RectMonitor.Height);
//        }
//        else
//        {
//            window.SetWindowSize(1024, 768);
//        }
//#if !DEBUG
//        Window.CenterOnScreen();
//#endif
//        Window.Title = "Crypto Portfolio Tracker";
//        Window.SetIcon(App.appPath + "\\Assets\\AppIcons\\CryptoPortfolioTracker.ico");
//        Window.SetTitleBar(null);
//        if (Window.Content is FrameworkElement frameworkElement) frameworkElement.RequestedTheme = userPreferences.AppTheme;

//    }

    private void InitializeLogger()
    {
        string[] commandLineArgs = Environment.GetCommandLineArgs();

        if (commandLineArgs.Length > 1 && commandLineArgs[1].Substring(1).ToLower() == "log")
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
               
               .CreateLogger();
#else

            Log.Logger = new LoggerConfiguration()
                        .WriteTo.File(App.appDataPath + "\\log.txt",
                           rollingInterval: RollingInterval.Day,
                           retainedFileCountLimit: 3,
                           outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level:u3}]  {SourceContext:lj}  {Message:lj}{NewLine}{Exception}")
                  .CreateLogger();
#endif
            App.userPreferences.AttachLogger();
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(App).Name.PadRight(22));
            Log.Information("------------------------------------");
            Log.Information("Started Crypto Portfolio Tracker {0}", App.ProductVersion);
        }
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }

    private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new NotImplementedException();
    }

}
