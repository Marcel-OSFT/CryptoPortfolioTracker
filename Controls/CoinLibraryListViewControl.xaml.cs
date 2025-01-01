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
        SetupTeachingTips();
    }

    private void SetupTeachingTips()
    {
        var teachingTipInitial = _viewModel._preferencesService.GetTeachingTip("TeachingTipBlank");
        var teachingTipNarr = _viewModel._preferencesService.GetTeachingTip("TeachingTipNarrLibr");

        if (teachingTipInitial == null || !teachingTipInitial.IsShown)
        {
            _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrLibr");
        }
        else if (teachingTipNarr != null && !teachingTipNarr.IsShown)
        {
            MyTeachingTipNarr.IsOpen = true;
        }
    }

    private void OnGetItClickedNarr(object sender, RoutedEventArgs e)
    {
        // Handle the 'Get it' button click
        MyTeachingTipNarr.IsOpen = false;
        _viewModel._preferencesService.SetTeachingTipAsShown("TeachingTipNarrLibr");
        // Navigate to the new feature or provide additional information
    }

    
}
