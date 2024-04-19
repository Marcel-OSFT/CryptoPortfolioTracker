using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
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
        
        appW.Resize(new Windows.Graphics.SizeInt32 { Width = 500, Height = 300 });
        presenter.SetBorderAndTitleBar(false, false);
#if !DEBUG
        this.CenterOnScreen();
#endif
        
    }

    
}
