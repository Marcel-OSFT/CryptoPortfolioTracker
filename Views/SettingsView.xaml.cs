using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

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
