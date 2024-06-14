using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;

namespace CryptoPortfolioTracker.Controls;

public partial class AssetTransactionsListViewControl : UserControl
{
    public readonly AssetsViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//
    public AssetTransactionsListViewControl()
    {
        InitializeComponent();
        _viewModel = AssetsViewModel.Current;
        DataContext = _viewModel;
    }

    private void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            VisualStateManager.GoToState(sender as Control, "ShowNote", true);
        }

    }
    private void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
        {
            VisualStateManager.GoToState(sender as Control, "HideNote", true);
        }

    }
    
}