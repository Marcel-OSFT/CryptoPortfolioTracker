using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Models;

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
            if (args.ClickedItem == null || ((GridView)sender).SelectedIndex < 0) { return; }
            // Forward event to View
            AssetsView.Current.AssetAccountsGridView_ItemClicked(sender, args);
        }


    }
    
}
