using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CryptoPortfolioTracker;
using CryptoPortfolioTracker.Views;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using CommunityToolkit.Common;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class AssetsListViewControl : UserControl
    {
        public readonly AssetsViewModel _viewModel;
        public static AssetsListViewControl Current;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public AssetsListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            _viewModel = AssetsViewModel.Current;
            DataContext = _viewModel;
            
        }

        private void AssetsListView_ItemClick(object sender, ItemClickEventArgs args)
        {
            if (args.ClickedItem == null || ((ListView)sender).SelectedIndex < 0) { return; }

            // Forward this event to the View
            AssetsView.Current.AssetsListView_ItemClicked(sender, args);
        }
    }
}