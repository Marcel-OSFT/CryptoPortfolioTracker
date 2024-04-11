using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Views;
using LanguageExt.ClassInstances;

//using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;



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

       
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            if (App.userPreferences.IsCheckForUpdate) CheckUpdateNow();

        }

        public async Task CheckUpdateNow()
        {
            AppUpdater appUpdater = new();
            var result = await appUpdater.Check(App.VersionUrl, App.ProductVersion);

            if (result == AppUpdaterResult.NeedUpdate)
            {
                var dlgResult = await ShowMessageDialog("Update Checker", "New version available. Do you want to download it? In the meanwhile you can continue using the app. You will be notified when the download has finished.", "Download", "Cancel");
                if (dlgResult == ContentDialogResult.Primary)
                {
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

                        var installRequest = await ShowMessageDialog("Download Succesfull", "The setup file has been saved in your Downloads folder. Click 'Install' to proceed with th einstallation. The application will be closed automatically.", "Install", "Cancel");
                        if (installRequest == ContentDialogResult.Primary)
                        {
                            appUpdater.ExecuteSetupFile();
                        }
                    }
                    else
                    {
                        await ShowMessageDialog("Downloading Setup File failed", "Update will be postponed", "Close");
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
            }
            else
            {
                if (selectedItem != null)
                {
                    pageType = Type.GetType("CryptoPortfolioTracker.Views." + (string)selectedItem.Tag);
                    if (pageType != null) LoadView(pageType);
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

