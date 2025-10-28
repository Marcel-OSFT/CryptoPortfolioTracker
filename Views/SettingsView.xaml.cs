using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace CryptoPortfolioTracker.Views;


[ObservableObject]
public partial class SettingsView : Page, IDisposable
{
    public readonly SettingsViewModel _viewModel;
    public static SettingsView Current;

    [ObservableProperty] bool isCardExpanded;
    [ObservableProperty] bool isCardEnabled;

    public SettingsView(SettingsViewModel viewModel)
    {
        Current = this;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        VersionNumber.Text = AppConstants.ProductVersion;
        isCardExpanded = false;
        isCardEnabled = true;
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_viewModel != null)
            {
                //_viewModel.Terminate();
                //_viewModel = null;
            }
        }
    }

}
