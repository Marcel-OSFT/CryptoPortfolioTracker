using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Media.MediaProperties;

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

}
