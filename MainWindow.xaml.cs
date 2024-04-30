using System.Linq;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace CryptoPortfolioTracker;

public sealed partial class MainWindow : Window
{
    private WindowManager _manager;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    private void ConfigureWindow()
    {
        _manager = WindowManager.Get(this);
        _manager.MinWidth = 800;
        _manager.MinHeight = 450;
        _manager.PersistenceId = "MainWindow";
        
        var monitor = MonitorInfo.GetDisplayMonitors().FirstOrDefault();
        if (monitor != null && monitor.RectMonitor.Width <= 1024 && monitor.RectMonitor.Height <= 768)
        {
            this.Maximize();
        }
        else
        {
            _manager.Height = 768;
            _manager.Width = 1024;
        }
#if !DEBUG
        this.CenterOnScreen();
        //Window.CenterOnScreen();
#endif
        _manager.AppWindow.Title = "Crypto Portfolio Tracker";
        _manager.AppWindow.SetIcon(App.appPath + "\\Assets\\AppIcons\\CryptoPortfolioTracker.ico");
        SetTitleBar(null);
        Content = new MainPage(); ;

        if (Content is FrameworkElement frameworkElement)
        {
            frameworkElement.RequestedTheme = App.userPreferences.AppTheme;
        }
    }




}
