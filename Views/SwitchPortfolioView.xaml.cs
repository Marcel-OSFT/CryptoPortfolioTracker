using CommunityToolkit.Mvvm.DependencyInjection;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;

namespace CryptoPortfolioTracker.Views
{
    public partial class SwitchPortfolioView : Page
    {
        public SwitchPortfolioViewModel _viewModel { get; }
        public static SwitchPortfolioView Current;

        public SwitchPortfolioView(SwitchPortfolioViewModel viewModel)
        {
            Current = this;
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
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
                    _viewModel.SelectedPortfolio = (Portfolio)selectedItem;
                }
            }
        }

        private void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                VisualStateManager.GoToState(sender as Control, "ShowBlue", true);
            }

        }
        private void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                VisualStateManager.GoToState(sender as Control, "ShowGreen", true);
            }

        }



        private void ShowBlue(object sender)
        {
            VisualStateManager.GoToState(sender as Control, "ShowBlue", true);
        }




        private void PortfoliosLV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SelectAnimation_Completed(object sender, object e)
        {
            Debug.WriteLine("SelectAnimation completed");
        }

        private void DeselectAnimation_Completed(object sender, object e)
        {
            Debug.WriteLine("DeselectAnimation completed");
        }





    }
}
