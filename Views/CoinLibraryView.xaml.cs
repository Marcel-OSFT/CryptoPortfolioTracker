
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using System;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views
{
    public sealed partial class CoinLibraryView : Page, IDisposable
    {
        public readonly CoinLibraryViewModel _viewModel;
        public static CoinLibraryView Current;

        public CoinLibraryView(CoinLibraryViewModel viewModel)
        {
            Current = this;
            this.InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        //private async void Button_Click_AddCoinDialog(object sender, RoutedEventArgs e)
        //{
        // //   await _viewModel.ShowAddCoinDialog();
        //}

        public void Dispose()
        {

        }
    }

}
