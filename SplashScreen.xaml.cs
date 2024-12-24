using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace CryptoPortfolioTracker;

public sealed partial class SplashScreen : Window
{
    public SplashScreen()
    {
        InitializeComponent();
        Title = string.Empty;
        
        var versionParts = App.ProductVersion.Split('.');


        versionTxt.Text = versionParts[0] + "." + versionParts[1]+ "." + versionParts[2];

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var WndID = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appW = AppWindow.GetFromWindowId(WndID);
        var presenter = appW.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);
        }
        appW.IsShownInSwitchers = false;
        appW.Resize(new Windows.Graphics.SizeInt32 { Width = 500, Height = 300 });

#if !DEBUG
        this.CenterOnScreen();
#endif
        
    }
    
}
