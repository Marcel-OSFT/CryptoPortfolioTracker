using CryptoPortfolioTracker.Models;
using System.Linq;
using Microsoft.UI.Xaml;
using WinUIEx;
//using CoinGecko.Clients;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private WindowManager _manager;
        public MainWindow()
        {
            this.InitializeComponent();
            ConfigureWindow();
        }

        private void ConfigureWindow()
        {
            
            _manager = WindowManager.Get(this);

            _manager.MinWidth = 800;
            _manager.MinHeight = 450;
            _manager.PersistenceId = "MainWindow";
            
            var monitor = MonitorInfo.GetDisplayMonitors().FirstOrDefault();
            if (monitor != null && monitor.RectMonitor.Width <= 1024 && monitor.RectMonitor.Height <= 768)
            {
                //_manager.Height = monitor.RectMonitor.Height;
                //_manager.Width = monitor.RectMonitor.Width;
                this.Maximize();
                //window.SetWindowSize(monitor.RectMonitor.Width, monitor.RectMonitor.Height);
            }
            else
            {
                _manager.Height = 768;
                _manager.Width = 1024;
                //window.SetWindowSize(1024, 768);
            }
#if !DEBUG
            this.CenterOnScreen();
            //Window.CenterOnScreen();
#endif
            _manager.AppWindow.Title = "Crypto Portfolio Tracker";
            _manager.AppWindow.SetIcon(App.appPath + "\\Assets\\AppIcons\\CryptoPortfolioTracker.ico");
            this.SetTitleBar(null);
            this.Content = new MainPage(); ;

            if (this.Content is FrameworkElement frameworkElement) frameworkElement.RequestedTheme = App.userPreferences.AppTheme;

        }




    }
}
