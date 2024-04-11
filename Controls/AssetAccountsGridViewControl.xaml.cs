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
       private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetGridViewItemWidth();
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
                int longestNameLength = 0;
                foreach (var item in AssetAccountsGridView.Items)
                {
                    if ((item as AssetAccount).Name.Length > longestNameLength)
                    {
                        longestNameLength = (item as AssetAccount).Name.Length;
                    }
                }
                //** the Tag is used for binding with the width of the Name Textbox

                AssetAccountsGridView.Tag = longestNameLength * ((int)App.userPreferences.FontSize + 7);
            };
        }


    }

}
