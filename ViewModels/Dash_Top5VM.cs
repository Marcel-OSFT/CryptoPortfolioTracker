using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;

using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;
using WinUI3Localizer;
using System.Globalization;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty] ObservableCollection<Coin> topWinners;
    [ObservableProperty] ObservableCollection<Coin> topLosers;

    private void ConstructTop5()
    {

    }

    public async Task GetTop5()
    {
        TopWinners = _dashboardService.GetTopWinners();
        TopLosers = _dashboardService.GetTopLosers();
    }



}
