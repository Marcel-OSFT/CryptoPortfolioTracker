using System;
using System.Diagnostics;
using CryptoPortfolioTracker.ViewModels;
using LiveChartsCore;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class GraphicView : Page
{
    public readonly GraphicViewModel _viewModel;
    public static GraphicView Current;

    public GraphicView(GraphicViewModel viewModel)
    {
        Current = this;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
    }

    private void View_Loading(Microsoft.UI.Xaml.FrameworkElement sender, object args)
    {
        _viewModel.InitializeGraph();
    }
    private void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }
}
