using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class AssetTransactionsListViewControl : UserControl
    {
        public readonly AssetsViewModel _viewModel;
        public static AssetTransactionsListViewControl Current;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public AssetTransactionsListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            _viewModel = AssetsViewModel.Current;
            DataContext = _viewModel;

        }


        private void AssetsListView_ItemClick(object sender, ItemClickEventArgs args)
        {
            //if (args.ClickedItem == null || ((ListView)sender).SelectedIndex < 0) { return; }

            //// Forward this event to the View
            //AssetsView.Current.AssetsListView_ItemClicked(sender, args);

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
}