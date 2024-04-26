using System.Diagnostics;
using CryptoPortfolioTracker.Models;
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
        /// <summary>
        /// The width of the gridview itemcontainer is set by the width of the first element. 
        /// To prevent that other (longer) names will be truncated, the elngth of the longest name will be 
        /// determined and the widt of the container set accordingly.
        /// </summary>
        private void SetGridViewItemWidth()
        {
            if (AssetAccountsGridView.ItemsSource != null && AssetAccountsGridView.Items.Count > 0)
            {
                int longestNameLength = 12; // 12 is the minimum width
                foreach (var item in AssetAccountsGridView.Items)
                {
                    if ((item as AssetAccount).Name.Length > longestNameLength)
                    {
                        longestNameLength = (item as AssetAccount).Name.Length;
                    }
                }
                var scale = AssetsView.Current.XamlRoot.RasterizationScale;
                //** the Tag is used for binding with the width of the Name Textbox
                switch ((int)App.userPreferences.FontSize)
                {
                    case 0: //small
                        {
                            AssetAccountsGridView.Tag = Scale * longestNameLength * 7;
                            break;
                        }
                    case 1: //normal
                        {
                            AssetAccountsGridView.Tag = scale * longestNameLength * 8;
                            break;
                        }
                    case 2: //large
                        {
                            AssetAccountsGridView.Tag = scale * longestNameLength * 9;
                            break;
                        }
                }
                AssetAccountsGridView.SelectedIndex = 0;
            };
        }

        private void GridView_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            if (e.NewSize.Height > e.PreviousSize.Height)

                SetGridViewItemWidth();
        }
    }

}
