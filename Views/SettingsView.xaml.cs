using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace CryptoPortfolioTracker.Views;

public partial class SettingsView : Page, IDisposable
{
    public readonly SettingsViewModel _viewModel;

    public SettingsView(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        VersionNumber.Text = App.ProductVersion;
    }

    private void View_Unloaded(object sender, RoutedEventArgs e)
    {
    }

    public void Dispose()
    {
       
    }
}
