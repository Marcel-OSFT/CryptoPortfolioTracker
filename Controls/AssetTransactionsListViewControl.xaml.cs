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
using CommunityToolkit.WinUI;
using CryptoPortfolioTracker.Enums;
using Windows.UI.Popups;


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
            if (args.ClickedItem == null || ((ListView)sender).SelectedIndex < 0) { return; }

            // Forward this event to the View
            AssetsView.Current.AssetsListView_ItemClicked(sender, args);

        }

        private void AssetTransactionListView_Loaded(object sender, RoutedEventArgs e)
        {
            AssetTransaction.commandDel.ExecuteRequested += DeleteCommand_ExecuteRequested;
            AssetTransaction.commandEdit.ExecuteRequested += EditCommand_ExecuteRequested;
        }

        
        private async void EditCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            // forward to the View
            // AssetAccount selectedAssetAcount =(AssetAccount)(AssetsView.Current.MyAssetAccountsGridViewControl.AssetAccountsGridView.SelectedItem);
            await _viewModel.ShowTransactionDialog(DialogAction.Edit, (int)args.Parameter );
        }

        private async void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            await _viewModel.DeleteTransaction((int)args.Parameter);
        }

        private void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
               VisualStateManager.GoToState((sender as Control), "ShowNote", true);       
            }

        }
        private void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
               VisualStateManager.GoToState((sender as Control), "HideNote", true);
            }

        }
    }
}