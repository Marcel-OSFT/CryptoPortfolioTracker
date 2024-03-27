

using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using System.Management.Automation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using CryptoPortfolioTracker.Models;
using System.Security.Cryptography;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection;
using Windows.UI.Core;
using System.Linq;
using System.Diagnostics;
using Microsoft.UI.Xaml.Controls.Primitives;
using CryptoPortfolioTracker.Dialogs;



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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            nvSample.SelectedItem = nvSample.MenuItems.OfType<Microsoft.UI.Xaml.Controls.NavigationViewItem>().First();

            //LoadView(typeof(AssetsView));
        }
      
        private async void NavigationView_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            NavigationViewItem selectedItem = (Microsoft.UI.Xaml.Controls.NavigationViewItem)args.SelectedItem;
            
            //-- TO DO help window 
            if ((string)selectedItem.Tag == "HelpView")
            {
                MsgBoxDialog dialog = new MsgBoxDialog("© MK-OSFT (mk_osft@hotmail.com) \r\nTo be implemented -> open pdf help file");
                var test = contentFrame.Content.GetType();
                dialog.XamlRoot = contentFrame.XamlRoot;
                dialog.Title = "Help";
                var result = await dialog.ShowAsync();
                return;

            }

            if (args.IsSettingsSelected)
            {
                //contentFrame.Navigate(typeof(SampleSettingsPage));
            }
            else
            {
                
                if (selectedItem != null)
                {
                    Type pageType = Type.GetType("CryptoPortfolioTracker.Views." + (string)selectedItem.Tag);
                    Debug.WriteLine("LoadView: " + (string)selectedItem.Tag);
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
    }

}

