﻿using CryptoPortfolioTracker.Infrastructure;
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.ApplicationModel.Activation;
using WinUIEx;

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
        //public const string CoinGeckoApiKey = "CG-Rrum93jyGvmSasVYijB4SegP";
        //public const string CoinGeckoApiKey = "CG-2K3WZ2mjbpGtCseUByHU1Qhc"; // old
        //public const string PublicApiPath = "https://api.coingecko.com/api/v3/";
        //public const string ProApiPath = "https://pro-api.coingecko.com/api/v3/";
        public static string ApiPath = "https://api.coingecko.com/api/v3/";

        public static string appPath;
        public static string appDataPath;

        //public static CultureInfo cultureInfoNl = new CultureInfo("nl");
        //public static CultureInfo cultureInfoEn = new CultureInfo("en");

        //public static CultureInfo CultureInfo;
        public static UserPreferences userPreferences;
        public static bool isReadingUserPreferences;

        public static IServiceProvider Container { get; private set; }

        private string download_link = null;

        public App()
        {
            this.InitializeComponent();
            SetAppPaths();
            GetUserPreferences();
            Container = RegisterServices();

            var context = App.Container.GetService<PortfolioContext>();

            context.Database.EnsureCreated();
            //if (context.Coins.ToList()==null) InitializeWithMocks();

            //var exists = HTTP_URLExists("https://marcel-osft.github.io/CryptoPortfolioTracker/CryptoPortfolioTracker_setup.exe");
            

        }


        public string CheckForUpdates()
        {
            /* Direct download link for the text file in your server */
            var version_file = "http://www.yourserver.com/appname/update/version.txt";

            /* Temporary output file to work with (located in AppData)*/
            var temp_version_file = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\appname_version.txt";

            /* Use the WebClient class to download the file from your server */
            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadFile(address: version_file, fileName: temp_version_file);
                }
                catch (Exception)
                {
                    /* Handle exceptions */
                    return "error";
                }
            }

            /* Check if temporary file was downloaded of not */
            if (File.Exists(temp_version_file))
            {
                /* Get the file content and split it in two */
                string[] version_data = File.ReadAllText(temp_version_file).Split('=');

                /* Variable to store the app new version */
                string version = version_data[0];

                /* Store the download link in the global variable already created */
                download_link = version_data[1];

                /* Compare the app current version with the version from the downloaded file */
                if (Application.ProductVersion.ToString() == version)
                {
                    return "updated";
                }
                else
                {
                    return "needs_update";
                }
            }

            /* Delete the temporary file after using it */
            if (File.Exists(temp_version_file))
            {
                File.Delete(temp_version_file);
            }
        }

        public async void RunUpdateCheck()
        {
            /* Maybe show a message to the user */
            labelUpdateState.Text = "Checking for updates. Please wait...";

            /* Variable to get the result from the check function */
            string result = await Task.Run(() => CheckForUpdates());

            /* Evaluate the result */
            switch (result)
            {
                case "error":
                    /* Do something here, maybe just notify the user */
                    break;

                case "updated":
                    /* Do something here, maybe just notify the user */
                    break;

                case "needs_update":
                    /* Perform the update operation, maybe just download
                    *  the new version with any installed browser or 
                    *  implement a download function with progressbar 
                    *  and the like, that's your choice.
                    *
                    *  Example:
                    */
                    Process.Start(download_link);
                    break;
            }

            /* (Optional)
            *  Set the 'download_link' variable to null for future use. 
            */
            download_link = null;

            /* (Optional - in case you have a backup feature)
            *  Tell the user to backup any data that may be saved before installing
            *  the new version in case the data is stored in the app install folder,
            *  I recommend using the AppData folder for saving user data.
            */
            labelUpdateState.Text = "Please backup your data before installing the new version";
        }

        private void GetUserPreferences()
        {
            isReadingUserPreferences = true;    
            userPreferences = new UserPreferences();

            if (File.Exists(appDataPath + "\\prefs.xml"))
            {
                XmlSerializer mySerializer = new XmlSerializer(typeof(UserPreferences));
                FileStream myFileStream = new FileStream(appDataPath + "\\prefs.xml", FileMode.Open);

                userPreferences = (UserPreferences)mySerializer.Deserialize(myFileStream);

                // CultureInfo = userPreferences.CultureLanguage == "nl" ? cultureInfoNl : cultureInfoEn;
                //CultureInfo.CurrentCulture = new CultureInfo(userPreferences.CultureLanguage);
                //CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(userPreferences.CultureLanguage); //App.cultureInfoNl;
                //CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(userPreferences.CultureLanguage); //App.cultureInfoNl;
            }
            else
            {
                userPreferences.CultureLanguage = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator == "," ? "nl" : "en";
                userPreferences.IsHidingZeroBalances = false;
                //SaveUserPreferences();
            }
            isReadingUserPreferences = false;
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

        public void GetCurrentCulture()
        {
            //CultureInfo = cultureInfoEn;
            //CultureInfo.DefaultThreadCurrentCulture = CultureInfo;
            //CultureInfo.DefaultThreadCurrentUICulture = CultureInfo;

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

                if (args.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                m_window.Content = rootFrame;
            }
            m_window.SetWindowSize(1200, 1000);
            m_window.Title = "Crypto Portfolio Tracker";

            //-- TO DO icon path below

            m_window.SetIcon(App.appPath + "\\Assets\\AppIcons\\CryptoPortfolioTracker.ico");
            m_window.SetTitleBar(null);
            m_window.Activate();

            //if (args.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
            //{
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
            }
            //    // Ensure the current window is active
            //    
            //}

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

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>

        /// <param name="e">Details about the suspend request.</param>
        //private void OnSuspending(object sender, SuspendingEventArgs e)
        //{
        //    var deferral = e.SuspendingOperation.GetDeferral();
        //    //TODO: Save application state and stop any background activity
        //    deferral.Complete();
        //}

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void InitializeWithMocks()
        {
            //var context = new PortfolioContext();
            var context = App.Container.GetService<PortfolioContext>();

            context.Database.EnsureCreated();

            AccountMockService myMockAccount = new AccountMockService();

            context.Coins.Add(CoinMockService.MockCoin1);
            context.Coins.Add(CoinMockService.MockCoin2);

            context.Accounts.Add(myMockAccount.MockAccount1);
            context.Accounts.Add(myMockAccount.MockAccount2);

            context.SaveChanges();

        }

    }
}
