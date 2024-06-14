using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class SettingsView : Page
{
    public readonly SettingsViewModel _viewModel;

    public SettingsView(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        VersionNumber.Text = App.ProductVersion;
    }

}
