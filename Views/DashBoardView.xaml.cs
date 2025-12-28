using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using TemperatureMonitor.ViewModels;
using TemperatureMonitor.Controls;
using TemperatureMonitor.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System;

namespace TemperatureMonitor.Views;
[ObservableObject]
public partial class DashboardView : Page, IDisposable
{
   private readonly DashboardViewModel _viewModel;
    public static DashboardView? Current;
   

    public DashboardView(DashboardViewModel dashboardVm)
    {
        Current = this;
        InitializeComponent();
        _viewModel = dashboardVm;
        DataContext = _viewModel;
    }

    private async void View_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _viewModel.ViewLoading();
    }

    private void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        //_viewModel.ViewLoading();
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
                _viewModel.Terminate();
                //_viewModel = null;
            }
        }
    }

    private void DaySelector_ModeChanged(object sender, TemperatureMonitor.Enums.DaySelectorMode e)
    {
        // handle mode change (e == DaySelectorMode.Now or DaySelectorMode.Day)
        if (e == null) return;
        _viewModel?.CurrentMode = e;
    }

    private void DaySelector_SelectedDateChanged(object sender, DateTime e)
    {
        // handle selected date change (e is the new SelectedDate)
        if (e == null) return;
        _viewModel?.SelectedDate = e;
    }
}
