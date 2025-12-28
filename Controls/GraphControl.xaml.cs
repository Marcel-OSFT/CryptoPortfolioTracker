using TemperatureMonitor.ViewModels;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView.SKCharts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

namespace TemperatureMonitor.Controls;

public partial class GraphControl : UserControl, INotifyPropertyChanged
{
    public readonly DashboardViewModel _viewModel;

    //***********************************************//
    //** All databound fields are in the viewModel**//
    //*********************************************//

    public GraphControl()
    {
        _viewModel = DashboardViewModel.Current ?? throw new InvalidOperationException("DashboardViewModel.Current is null");
        DataContext = _viewModel;
        InitializeComponent();
        _viewModel.ConstructGraph();
        // initialize combo selection to reflect the chart's current ZoomMode
        Loaded += (_, __) => InitializeZoomModeSelector();
    }

    private void InitializeZoomModeSelector()
    {
        // Map chart's ZoomMode to the ComboBox selection
        var mode = thisChart?.ZoomMode ?? ZoomAndPanMode.X;
        string tag = mode switch
        {
            ZoomAndPanMode.X => "X",
            ZoomAndPanMode.Y => "Y",
            ZoomAndPanMode.Both => "XY",
            _ => "X"
        };

        foreach (var item in cmbZoomMode.Items)
        {
            if (item is ComboBoxItem cbi && (cbi.Tag?.ToString() ?? "") == tag)
            {
                cmbZoomMode.SelectedItem = cbi;
                break;
            }
        }
    }
    private void cmbZoomMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbZoomMode.SelectedItem is not ComboBoxItem item) return;
        var tag = item.Tag?.ToString() ?? item.Content?.ToString() ?? "X";

        switch (tag)
        {
            case "X":
                thisChart.ZoomMode = ZoomAndPanMode.X;
                break;
            case "Y":
                thisChart.ZoomMode = ZoomAndPanMode.Y;
                break;
            case "XY":
            default:
                thisChart.ZoomMode = ZoomAndPanMode.Both;
                break;
        }
    }
    private void btnResetZoom_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Rebuild axes in the ViewModel so they return to their default configuration
            _viewModel.ConstructGraph();

            // Make sure the chart uses the refreshed axes
            thisChart.XAxes = _viewModel.XAxesGraph;
            thisChart.YAxes = _viewModel.YAxesGraph;

            // Reset zoom mode to current selection (or default to X)
            InitializeZoomModeSelector();

            // If the chart exposed a dedicated ResetZoom API in the future you can call it here.
            // Re-assigning axes from the VM returns the axis ranges to their initial state,
            // which effectively resets any user zoom/pan.
        }
        catch (Exception)
        {
            // swallow — UI should not crash on reset, but log if you have a logger available
        }
    }
    private async void Control_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
       // await _viewModel.GraphControlLoading();
    }

    private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.ChangeLabelSizeGraph(e);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.GraphControlLoaded();
    }
}
