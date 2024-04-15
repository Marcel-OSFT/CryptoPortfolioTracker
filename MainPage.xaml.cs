using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Views;
using LanguageExt.ClassInstances;

//using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;



// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>

    public partial class MainPage : Page
    {
        public static MainPage Current;
        private IServiceScope _currentServiceScope;
        public  LogWindow logWindow;

        private ILogger Logger { get; set; }

        public MainPage()
        {
            ConfigureLogger();
           
            //GetUserPreferences();

            this.InitializeComponent();
            Current = this;
           // if (App.userPreferences.IsCheckForUpdate) CheckUpdateNow();
         }

        private void ConfigureLogger()
        {
            if (App.isLogWindowEnabled)
            {
                logWindow = new LogWindow();
                logWindow.Title = Assembly.GetExecutingAssembly().GetName().Name.ToString() + " Event Log";
                logWindow.Activate();
            }
            // Logger = Log.Logger.ForContext<MainPage>();
            Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(MainPage).Name.PadRight(22));
        }

        public async Task CheckUpdateNow()
        {
            Logger.Information("Checking for updates");
            ResourceLoader rl = new();
            AppUpdater appUpdater = new();
            var result = await appUpdater.Check(App.VersionUrl, App.ProductVersion);

            if (result == AppUpdaterResult.NeedUpdate)
            {
                Logger.Information("Update Available");

                var dlgResult = await ShowMessageDialog(
                    rl.GetString("Messages_UpdateChecker_NewVersionTitle"),
                    rl.GetString("Messages_UpdateChecker_NewVersionMsg"),
                    rl.GetString("Common_DownloadButton"),
                    rl.GetString("Common_CancelButton"));

                if (dlgResult == ContentDialogResult.Primary)
                {
                    Logger.Information("Downloading update");
                    var downloadResult = await appUpdater.DownloadSetupFile();
                    //Download is running async, so the user can continue to do other stuff
                    if (downloadResult == AppUpdaterResult.DownloadSuccesfull)
                    {
                        //*** Download is doen, wait till there is no other dialog box open
                        while (App.isBusy)
                        {
                            await Task.Delay(5000);
                            ;
                        }
                        Logger.Information("Download Succesfull");
                        var installRequest = await ShowMessageDialog(
                            rl.GetString("Messages_UpdateChecker_DownloadSuccesTitle"),
                            rl.GetString("Messages_UpdateChecker_DownloadSuccesMsg"),
                            rl.GetString("Common_InstallButton"),
                            rl.GetString("Common_CancelButton"));
                        if (installRequest == ContentDialogResult.Primary)
                        {
                            Logger.Information("Closing Application and Installing Update");
                            appUpdater.ExecuteSetupFile();
                            Environment.Exit(0);
                        }
                    }
                    else
                    {
                        Logger.Warning("Download failed");
                        await ShowMessageDialog(
                            rl.GetString("Messages_UpdateChecker_DownloadFailedTitle"),
                            rl.GetString("Messages_UpdateChecker_DownloadFailedMsg"),
                            rl.GetString("Common_CloseButton"));
                    }
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            nvSample.SelectedItem = nvSample.MenuItems.OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>().First();

            //LoadView(typeof(AssetsView));
        }

        private async void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            Type pageType;
            if (args.IsSettingsSelected)
            {
                pageType = Type.GetType("CryptoPortfolioTracker.Views.SettingsView");
                if (pageType != null) LoadView(pageType);
                Logger.Information("Navigated to {0}", "SettingsView");

            }
            else
            {
                if (selectedItem != null)
                {
                    pageType = Type.GetType("CryptoPortfolioTracker.Views." + (string)selectedItem.Tag);
                    if (pageType != null) LoadView(pageType);
                    Logger.Information("Navigated to {0}", (string)selectedItem.Tag); ;
                }
            }
        }
        private void LoadView(Type viewType)
        {
            if (_currentServiceScope != null)
            {
                _currentServiceScope.Dispose();
            }
            _currentServiceScope = App.Container.CreateAsyncScope();

            contentFrame.Content = _currentServiceScope.ServiceProvider.GetService(viewType);
        }
        public async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                XamlRoot = Current.XamlRoot,
                Content = message,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText
            };
            var dlgResult = await dialog.ShowAsync();
            return dlgResult;
        }


    }

}

