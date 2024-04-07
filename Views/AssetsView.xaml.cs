//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Views
{

    public partial class AssetsView : Page, IDisposable
    {
        public readonly AssetsViewModel _viewModel;
        public static AssetsView Current;
        private bool isExtendedView = false;

       
        public AssetsView(AssetsViewModel viewModel)// ** DI of viewModel into View
        {
            Current = this;
            _viewModel = viewModel;
            this.InitializeComponent();
            DataContext = _viewModel;
            MyAssetsListViewControl.AssetsListView.DataContext = _viewModel;
        }

        public void Dispose()
        {
            Debug.WriteLine("View Disposed");
        }



    }

}

