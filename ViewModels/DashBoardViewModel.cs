using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TemperatureMonitor.Controls;
using TemperatureMonitor.Converters;
using TemperatureMonitor.Dialogs;
using TemperatureMonitor.Enums;
using TemperatureMonitor.Models;
using TemperatureMonitor.Services;
using TemperatureMonitor.Views;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web.Provider;
using WinUI3Localizer;

namespace TemperatureMonitor.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    public static DashboardViewModel? Current;
    private static ILocalizer loc = Localizer.Get();
    private readonly IGraphService _graphService;
    private readonly Esp32Service _esp32Service;
    private readonly IMessenger _messenger;

    public Settings AppSettings => base.AppSettings; // expose AppSettings publicly so that it can be used in dialogs called by this ViewModel

    [ObservableProperty] public partial string LastTemperatureReading { get; set; } = string.Empty;

    [ObservableProperty] public partial string LastReadingTimestamp { get; set; } = string.Empty;


    [ObservableProperty] public partial DaySelectorMode CurrentMode { get; set; } = DaySelectorMode.Dag;
    [ObservableProperty] public partial DateTimeOffset SelectedDate { get; set; } = DateTimeOffset.Now;

    private DateTime nowStart;

    partial void OnCurrentModeChanged(DaySelectorMode oldValue, DaySelectorMode newValue)
    {
        Debug.WriteLine($"DashboardViewModel: CurrentMode changed from {oldValue} to {newValue}");
        UpdateSampleRateForMode(newValue);
        if (newValue == DaySelectorMode.Nu)
        {
            nowStart = DateTime.Now;
        }
    }

    private void UpdateSampleRateForMode(DaySelectorMode mode)
    {
        var sampleRateInSeconds = mode == DaySelectorMode.Nu ? AppSettings.SampleIntervalFastSeconds : AppSettings.SampleIntervalSlowMinutes * 60;
        _esp32Service.SetSampleRate(sampleRateInSeconds);
        // synchronize GraphUpdateService via Messenger
        MainPage.Current.DispatcherQueue.TryEnqueue(() =>
        {
            _messenger.Send(new CurrentModeChangedMessage(sampleRateInSeconds));
        });
    }

    async partial void OnSelectedDateChanged(DateTimeOffset oldValue, DateTimeOffset newValue)
    {
        Debug.WriteLine($"DashboardViewModel: SelectedDate changed from {oldValue} to {newValue}");
        await GetValuesGraph(DateOnly.FromDateTime(SelectedDate.Date));
        SetSeriesGraph();
    }


    public DashboardViewModel(Esp32Service esp32Service, IGraphService graphService, IMessenger messenger, Settings appSettings) : base(appSettings)
    {
        messenger.Register<GraphUpdatedMessage>(this, (r, m) =>
        {
            UpdateSeriesValues();
        });
        messenger.Register<SampleRateSettingChangedMessage>(this, (r, m) =>
        {
            if (m.mode == CurrentMode)
            {
                UpdateSampleRateForMode(CurrentMode);
            }
        });

        Current = this;
        _graphService = graphService;
        _esp32Service = esp32Service;
        _messenger = messenger;

    }

    

    /// <summary>  
    /// This method is called by the DashboardView_Loaded event.  
    /// </summary>  
    public void ViewLoading()
    {

    }

    public void Terminate()
    {
       
    }

    
}






