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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CryptoPortfolioTracker.Controls
{
    public partial class CoinLibraryListViewControl : UserControl
    {
        public readonly CoinLibraryViewModel _viewModel;
        public static CoinLibraryListViewControl Current;

        //***********************************************//
        //** All databound fields are in the viewModel**//
        //*********************************************//

        public CoinLibraryListViewControl()
        {
            this.InitializeComponent();
            Current = this;
            _viewModel = CoinLibraryViewModel.Current;
            DataContext = _viewModel;
        }

        private void CoinLibraryListView_Loaded(object sender, RoutedEventArgs e)
        {
            Coin.commandDel.ExecuteRequested += DeleteCommand_ExecuteRequested;
            Coin.commandAbout.ExecuteRequested += AboutCommand_ExecuteRequested;
            Coin.commandNote.ExecuteRequested += NoteCommand_ExecuteRequested;
        }


        private async void AboutCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            await _viewModel.ShowDescription((string)args.Parameter);
        }
        private async void NoteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {
            await _viewModel.ShowAddNoteDialog((string)args.Parameter);
        }
        private async void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        {           
            await _viewModel.DeleteCoin((string)args.Parameter);
        }
    }
}
