using CryptoPortfolioTracker.Dialogs;
//using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Diagnostics;
using System.Linq;



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
    }

}

