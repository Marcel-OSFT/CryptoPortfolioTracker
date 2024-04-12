using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel.Activation;
using WinUIEx;
using System.Windows;
using Windows.Devices.Enumeration;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker

{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>

        private static Window m_window;
        public const string CoinGeckoApiKey = "";
        public static string ApiPath = "https://api.coingecko.com/api/v3/";
        public static string appPath;
        public static string appDataPath;
        public static string ProductVersion;
        public const string VersionUrl = "https://marcel-osft.github.io/CryptoPortfolioTracker/current_version.txt";
        public static bool isBusy;
        public static UserPreferences userPreferences;
        public static bool isReadingUserPreferences;

        public static IServiceProvider Container { get; private set; }
        
        public App()
        {
            this.InitializeComponent();
            ProductVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            SetAppPaths();
            GetUserPreferences();
            Container = RegisterServices();
            var context = App.Container.GetService<PortfolioContext>();
            context.Database.EnsureCreated();
            userPreferences.SaveUserPreferences("_App");
        }
        
        private void GetUserPreferences()
        {
            userPreferences = new UserPreferences();
            try
            {
                if (File.Exists(appDataPath + "\\prefs.xml"))
                {
                    isReadingUserPreferences = true;
                    XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
                    FileStream myFileStream = new FileStream(appDataPath + "\\prefs.xml", FileMode.Open);

                    userPreferences = (UserPreferences)mySerializer.Deserialize(myFileStream);
                }
            }
            catch { }
            finally 
            { 
                isReadingUserPreferences = false; 
            }
        }

        public static void SaveUserPreferences()
        {
            XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
            StreamWriter myWriter = new StreamWriter(appDataPath + "\\prefs.xml");
            mySerializer.Serialize(myWriter, userPreferences);
            myWriter.Close();
        }
        private void SetAppPaths()
        {
            appPath = (System.IO.Path.GetDirectoryName(System.AppContext.BaseDirectory));

            appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\CryptoPortfolioTracker";
            if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);
            AppDomain.CurrentDomain.SetData("DataDirectory", appDataPath);
        }
        private IServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();

            //services.AddScoped<TransactionDialog>();

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

            services.AddDbContext<CoinContext>(options =>
            {
                options.UseSqlite("Data Source=|DataDirectory|sqlCPT.db");
            });

            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IAssetService, AssetService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ILibraryService, LibraryService>();
            services.AddScoped<IPriceUpdateBackgroundService, PriceUpdateBackgroundService>(serviceProvider =>
            {
                return new(TimeSpan.FromSeconds(300));
            });

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            Frame rootFrame = m_window.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                // Place the frame in the current Window
                m_window.Content = rootFrame;
            }
            var monitor = MonitorInfo.GetDisplayMonitors().FirstOrDefault();
            if (monitor != null && monitor.RectMonitor.Width <= 1024 && monitor.RectMonitor.Height <= 768)
            {
                m_window.SetWindowSize(monitor.RectMonitor.Width,monitor.RectMonitor.Height);
            }
            else
            {
                m_window.SetWindowSize(1024, 768);
            }
            m_window.CenterOnScreen();
            m_window.Title = "Crypto Portfolio Tracker";
            m_window.SetIcon(App.appPath + "\\Assets\\AppIcons\\CryptoPortfolioTracker.ico");
            m_window.SetTitleBar(null);
            m_window.Activate();
            //if (args.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
            //{
            if (rootFrame.Content == null)
            {
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
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
}
