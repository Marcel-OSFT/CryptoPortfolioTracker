
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.UI.Popups;
using Microsoft.Extensions.DependencyInjection;

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
        
        private async void Button_Click_AddCoinDialog(object sender, RoutedEventArgs e)
        {
            await _viewModel.ShowAddCoinDialog();           
        }

        public void Dispose()
        {
           
        }
    }

}
