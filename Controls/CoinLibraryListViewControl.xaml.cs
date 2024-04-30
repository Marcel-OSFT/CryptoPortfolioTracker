using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls;

public partial class CoinLibraryListViewControl : UserControl
{
    public readonly CoinLibraryViewModel _viewModel;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public static CoinLibraryListViewControl Current;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//
    public CoinLibraryListViewControl()
    {
        InitializeComponent();
        Current = this;
        _viewModel = CoinLibraryViewModel.Current;
        DataContext = _viewModel;
    }

}
