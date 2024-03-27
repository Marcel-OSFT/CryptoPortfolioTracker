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
using CryptoPortfolioTracker.Enums;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class AccountsListViewControl : UserControl
    {
        public readonly AccountsViewModel _viewModel;
        public static AccountsListViewControl Current;

        private bool isAssetListShown;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public AccountsListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            _viewModel = AccountsViewModel.Current;
        }
        private void AccountsListView_Loaded(object sender, RoutedEventArgs e)
        {         
            Account.commandDel.ExecuteRequested += DeleteCommand_ExecuteRequested;
            Account.commandEdit.ExecuteRequested += EditCommand_ExecuteRequested;
        }

        private async void EditCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            if (sender.CanExecute(null)) await _viewModel.ShowAccountDialog(DialogAction.Edit, (int)args.Parameter);
        }
        private async void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            await _viewModel.DeleteAccount((int)args.Parameter);
            
        }
       
        private void AccountsListView_ItemClick(object sender, ItemClickEventArgs args)
        {
            if (args.ClickedItem == null || ((ListView)sender).SelectedIndex < 0) { return; }

            //check if new selection is made. If so then hide commandbar
            if (((ListView)sender).SelectedItem == null || ((ListView)sender).SelectedItem != args.ClickedItem)
            {
                isAssetListShown = true;
            }
            else
            {
                isAssetListShown = false;
            }

            
            // Forward this event to the View
            AccountsView.Current.AccountsListView_ItemClicked(sender, args);
        }

        private void ListViewItem_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {   //prevent commandBar being used when in 'showAssets' mode
                if (isAssetListShown) VisualStateManager.GoToState((sender as Control), "HideCommandBar", true);
            }

        }
        private void ListViewItem_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Pen)
            {
                VisualStateManager.GoToState((sender as Control), "ShowCommandBar", true);
            }

        }

    }
}
