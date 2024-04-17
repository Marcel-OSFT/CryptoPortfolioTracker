using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            Debug.WriteLine("triggered");
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
                //** the Tag is used for binding with the width of the Name Textbox
                switch ((int)App.userPreferences.FontSize)
                {
                    case 0: //small
                        {
                            AssetAccountsGridView.Tag = longestNameLength * 7;
                            break;
                        }
                    case 1: //normal
                        {
                            AssetAccountsGridView.Tag = longestNameLength * 8;
                            break;
                        }
                    case 2: //large
                        {
                            AssetAccountsGridView.Tag = longestNameLength * 9;
                            break;
                        }
                }
            };
        }

        private void GridView_SizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
        {
            SetGridViewItemWidth();
        }
    }

}
