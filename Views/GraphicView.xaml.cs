using System;
using CryptoPortfolioTracker.ViewModels;
using LiveChartsCore;
using Microsoft.UI.Xaml.Controls;

namespace CryptoPortfolioTracker.Views;

public partial class GraphicView : Page, IDisposable
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
    private void View_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
    }

    public void Dispose()
    {
        Current = null;
    }
}
