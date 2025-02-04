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
using System.Collections.Generic;

namespace CryptoPortfolioTracker.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty] List<Coin> topWinners = new();
    [ObservableProperty] List<Coin> topLosers = new();

    

    /// <summary>   
    /// This method is called by the Top5Control_Loaded event.  
    /// </summary>
    public async Task Top5ControlLoaded()
    {
        await GetTop5();
    }
    public void Top5ControlUnloaded()
    {
        TopWinners.Clear();
        TopLosers.Clear();
    }

    public async Task GetTop5()
    {
        TopWinners = await _dashboardService.GetTopWinners();
        TopLosers = await _dashboardService.GetTopLosers();
    }

}
