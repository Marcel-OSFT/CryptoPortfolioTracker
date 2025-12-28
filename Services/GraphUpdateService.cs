using CommunityToolkit.Mvvm.Messaging;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.TaskScheduler;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using TemperatureMonitor;
using TemperatureMonitor.Models;
using TemperatureMonitor.ViewModels;
using Task = System.Threading.Tasks.Task;

namespace TemperatureMonitor.Services;

public class GraphUpdateService : IGraphUpdateService
{
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _timerTask;
    private readonly object _syncLock = new();
    private readonly Settings _appSettings;
    private readonly IGraphService _graphService;
    private readonly IMessenger _messenger;
    private readonly Esp32Service _esp32Service;

    public bool IsUpdating { get; private set; }

    public GraphUpdateService(Esp32Service esp32Service, IGraphService graphService, IMessenger messenger, Settings appSettings)
    {
        _appSettings = appSettings;
        _graphService = graphService;
        _messenger = messenger;
        _esp32Service = esp32Service;

        // create initial timer using minutes (adjust if your setting is seconds)
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(_appSettings.SampleIntervalSlowMinutes));

        // message handler schedules an async change (fire-and-forget pattern)
        messenger.Register<CurrentModeChangedMessage>(this, (r, m) =>
        {
            _ = ChangeSampleRateAsync(m.sampleRate); // sampleRate assumed to be seconds; change TimeSpan method accordingly
        });
    }

    public Task StartAsync()
    {
        lock (_syncLock)
        {
            if (_timerTask != null && !_timerTask.IsCompleted) return Task.CompletedTask;
            _cts = new CancellationTokenSource();

            // ensure _timer is present (it may have been created in ChangeSampleRateAsync)
            if (_timer == null)
                _timer = new PeriodicTimer(TimeSpan.FromMinutes(_appSettings.SampleIntervalSlowMinutes));

            // run the loop on the threadpool
            _timerTask = Task.Run(() => DoWorkAsync(_cts.Token), CancellationToken.None);
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? localCts;
        Task? localTask;
        PeriodicTimer? localTimer;

        lock (_syncLock)
        {
            localCts = _cts;
            localTask = _timerTask;
            localTimer = _timer;

            _cts = null;
            _timerTask = null;
            _timer = null;
        }

        if (localCts != null && !localCts.IsCancellationRequested)
            localCts.Cancel();

        if (localTask != null)
        {
            try
            {
                await localTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"GraphUpdateService stopped with exception: {ex}");
            }
        }

        try { localTimer?.Dispose(); } catch { }
        try { localCts?.Dispose(); } catch { }

        IsUpdating = false;
    }

    private async Task ChangeSampleRateAsync(int sampleRateInSeconds)
    {
        // stop previous loop and await completion
        await StopAsync().ConfigureAwait(false);

        // create new timer with requested interval
        lock (_syncLock)
        {
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Min(sampleRateInSeconds,300)));
            _cts = new CancellationTokenSource();
            _timerTask = Task.Run(() => DoWorkAsync(_cts.Token), CancellationToken.None);
        }

        Debug.WriteLine($"GraphUpdateService synchronized with sampleRate: {sampleRateInSeconds} seconds");
    }
    private async Task DoWorkAsync(CancellationToken token)
    {
        try
        {
            await CheckForNewTemperatures().ConfigureAwait(false);

            while (_timer != null && await _timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Debug.WriteLine("GraphUpdateService triggered");
                await CheckForNewTemperatures().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("GraphUpdateService cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GraphUpdateService stopped unexpected: {ex}");
        }
        finally
        {
            // leave IsUpdating toggled by CheckForNewTemperatures; ensure cleanup if needed
        }
    }
    

    private async Task<Result<bool>> CheckForNewTemperatures()
    {
        IsUpdating = true;
        try
        {
            //todo: read temperature_log from ESP32 via wifi

            if (await _esp32Service.GetTemperatureLogFromESP())
            {
                List<DataPoint> allTemperaturesOnMultipleDays = await TemperatureLogReader.ReadTemperatureLogFileAsync(Path.Combine(AppConstants.AppDataPath, "TemperatureLog.txt"));
                // archive all readings into daily files under AppConstants.AppDataPath\DailyArchive
                await DailyArchiveService.ArchiveAsync(allTemperaturesOnMultipleDays, AppConstants.AppDataPath, useLocalDate: true);

                // Append temperatures of the current day to CurrentDayTemperatures in graph service
                var today = DateOnly.FromDateTime(DateTime.Now);
                List<DataPoint> todaysTemperatures = allTemperaturesOnMultipleDays
                    .Where(p => DateOnly.FromDateTime(p.Timestamp.ToLocalTime()) == today)
                    .ToList();

                _graphService.AppendTemperaturesToCurrentDay(todaysTemperatures);

            }
            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            {
                _messenger.Send(new GraphUpdatedMessage());
            });
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("CheckForNewGraphData failed.");
            return new Result<bool>(ex);
        }
        finally
        {
            IsUpdating = false;
        }
    }

    public async Task<ContentDialogResult> ShowMessageDialog(string title, string message, string primaryButtonText = "OK", string closeButtonText = "")
    {
        var dialog = new ContentDialog()
        {
            Title = title,
            XamlRoot = MainPage.Current.XamlRoot,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            RequestedTheme = _appSettings.AppTheme
        };
        var dlgResult = await dialog.ShowAsync();
        return dlgResult;
    }

}

