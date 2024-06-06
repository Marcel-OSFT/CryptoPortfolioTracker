using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Media.MediaProperties;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls;

public partial class CoinLibraryListViewControl : UserControl
{
    public readonly CoinLibraryViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//
    public CoinLibraryListViewControl()
    {
        InitializeComponent();
        _viewModel = CoinLibraryViewModel.Current;
        DataContext = _viewModel;
    }

    private void Control_Unload(object sender, RoutedEventArgs e)
    {
        CoinLibraryListView = null;
        DataContext = null;
        ColumnHeadersAndListView = null;
    }
    
}
