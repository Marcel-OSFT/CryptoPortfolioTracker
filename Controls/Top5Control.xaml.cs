using CryptoPortfolioTracker.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Controls;

public partial class Top5Control : UserControl, INotifyPropertyChanged
{
    public readonly DashboardViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public Top5Control()
    {
        InitializeComponent();
        _viewModel = DashboardViewModel.Current ?? throw new InvalidOperationException("DashboardViewModel.Current is null");
        DataContext = _viewModel;
    }

    private async void Control_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        //await _viewModel.Top5ControlLoading();
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.Top5ControlLoaded();
    }

    private void Control_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
       // _viewModel.Top5ControlUnloaded();
    }



    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // Log the exception details (optional)
       
        // Prevent the application from crashing
        e.Handled = true;

        // Show a user-friendly message
        ShowErrorMessage(e.Message);
    }

    public void ShowErrorMessage(string message)
    {
        // Ensure execution on the UI thread
        MainPage.Current.DispatcherQueue.TryEnqueue(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "An error occurred",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = MainPage.Current.XamlRoot
            };

            await dialog.ShowAsync();
        });
    }

    
}
