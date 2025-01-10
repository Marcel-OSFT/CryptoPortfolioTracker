using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace CryptoPortfolioTracker.Controls
{
    public sealed partial class TestUserControlWorking : UserControl
    {
        public TestUserControlWorking()
        {
            this.InitializeComponent();
        }

        private void PortfoliosLV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PortfoliosListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (sender is ListView listView)
            {
                // Get the clicked item
                var selectedItem = listView.SelectedItem;
                if (selectedItem != null)
                {
                    // ShowBlue(sender);
                   // _viewModel.SelectedPortfolio = (Portfolio)selectedItem;
                }
            }
        }

        private void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                if (sender is Control control)
                {
                    VisualStateManager.GoToState(control, "ShowGreen", false);
                }
            }
        }
        private void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                if (sender is Control control)
                {
                    VisualStateManager.GoToState(control, "ShowTransparent", false);
                }
            }

        }






    }
}