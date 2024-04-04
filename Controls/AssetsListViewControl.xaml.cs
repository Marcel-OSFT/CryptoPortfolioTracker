using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class AssetsListViewControl : UserControl
    {
       // public readonly AssetsViewModel _viewModel;
        public static AssetsListViewControl Current;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public AssetsListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            //_viewModel = AssetsViewModel.Current;
            //DataContext = _viewModel;
        }

        private void AssetsListView_ItemClick(object sender, ItemClickEventArgs args)
        {
            //if (args.ClickedItem == null || ((ListView)sender).SelectedIndex < 0) { return; }

            //// Forward this event to the View
            //AssetsView.Current.AssetsListView_ItemClicked(sender, args);
        }

        private void AssetsListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender == null) return;
            (sender as ListView).ScrollIntoView((sender as ListView).SelectedItem);
        }

        
    }
}