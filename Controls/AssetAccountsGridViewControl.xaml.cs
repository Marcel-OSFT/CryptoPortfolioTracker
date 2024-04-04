using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public sealed partial class AssetAccountsGridViewControl : UserControl
    {
        public AssetAccountsGridViewControl()
        {
            this.InitializeComponent();
        }

        private void AssetAccountsGridView_ItemClick(object sender, ItemClickEventArgs args)
        {
            //if (args.ClickedItem == null || ((GridView)sender).SelectedIndex < 0) { return; }
            //// Forward event to View
            //AssetsView.Current.AssetAccountsGridView_ItemClicked(sender, args);
        }


    }

}
