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
    //[ObservableProperty] public partial ObservableCollection<Coin> TopWinners { get; set; } = new();
    //[ObservableProperty] public partial ObservableCollection<Coin> TopLosers { get; set; } = new();
    [ObservableProperty] public partial List<Coin> TopWinners { get; set; } = new();
    [ObservableProperty] public partial List<Coin> TopLosers { get; set; } = new();



    /// <summary>   
    /// This method is called by the Top5Control_Loaded event.  
    /// </summary>
    public async Task Top5ControlLoaded()
    {
        await GetTop5();
    }
    public void Top5ControlUnloaded()
    {
        //TopWinners = null;
        //TopLosers = null;

        //OnPropertyChanged(nameof(TopWinners));
        //OnPropertyChanged(nameof(TopLosers));

        //TopWinners = new();
        //TopLosers = new();
    }

    public async Task GetTop5()
    {
        
        TopWinners = new(await _dashboardService.GetTopWinners());
        TopLosers = new(await _dashboardService.GetTopLosers());
    }

}
