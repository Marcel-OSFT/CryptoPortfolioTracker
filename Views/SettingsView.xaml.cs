using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Storage;
using Windows.Foundation.Metadata;
using Windows.Foundation;

namespace CryptoPortfolioTracker.Views
{
    public partial class SettingsView : Page
    {
        public readonly SettingsViewModel _viewModel;
        public static SettingsView Current;

        public SettingsView(SettingsViewModel viewModel)
        {
            Current = this;
            _viewModel = viewModel;
            this.InitializeComponent();
            DataContext = _viewModel;  
            VersionNumber.Text = App.ProductVersion;
        }

        
    }

}
