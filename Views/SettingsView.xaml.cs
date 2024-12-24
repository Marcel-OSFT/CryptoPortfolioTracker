using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Views;


[ObservableObject]
public partial class SettingsView : Page
{
    public readonly SettingsViewModel _viewModel;

    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    public SettingsView(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        VersionNumber.Text = App.ProductVersion;
        isCardExpanded = false;
        isCardEnabled = true;
    }

}
