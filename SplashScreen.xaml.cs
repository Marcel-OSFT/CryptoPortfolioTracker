using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace CryptoPortfolioTracker;

public sealed partial class SplashScreen : Window
{

    public SplashScreen()
    {
        this.InitializeComponent();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        WindowId WndID = Win32Interop.GetWindowIdFromWindow(hwnd);
        AppWindow appW = AppWindow.GetFromWindowId(WndID);
        OverlappedPresenter presenter = appW.Presenter as OverlappedPresenter;
        presenter.IsAlwaysOnTop = true;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsResizable = false;
        appW.IsShownInSwitchers = false;
        this.Title = "";
        versionTxt.Text =App.ProductVersion;
        
        appW.Resize(new Windows.Graphics.SizeInt32 { Width = 500, Height = 300 });
        presenter.SetBorderAndTitleBar(false, false);
#if !DEBUG
        this.CenterOnScreen();
#endif
        
    }

    
}
