using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class CoinLibraryListViewControl : UserControl
    {
        public readonly CoinLibraryViewModel _viewModel;
        public static CoinLibraryListViewControl Current;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public CoinLibraryListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            _viewModel = CoinLibraryViewModel.Current;
            DataContext = _viewModel;
        }

    }
}
