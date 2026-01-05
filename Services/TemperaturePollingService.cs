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

public class TemperaturePollingService : ITemperaturePollingService
{
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _timerTask;
    private readonly object _syncLock = new();
    private readonly Settings _appSettings;
    private readonly IGraphService _graphService;
    private readonly IMessenger _messenger;
    private readonly Esp32Service _esp32Service;
    private readonly ITemperatureLoggerStore _temperatureLoggerStore;

    public bool IsUpdating { get; private set; }

    public TemperaturePollingService(Esp32Service esp32Service, IGraphService graphService, ITemperatureLoggerStore temperatureLoggerStore , IMessenger messenger, Settings appSettings)
    {
        _appSettings = appSettings;
        _graphService = graphService;
        _messenger = messenger;
        _esp32Service = esp32Service;
        _temperatureLoggerStore = temperatureLoggerStore;

        // create initial timer using minutes (adjust if your setting is seconds)
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        //_timer = new PeriodicTimer(TimeSpan.FromMinutes(_appSettings.SampleIntervalSlowMinutes));

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
                Debug.WriteLine($"TemperaturePollingService stopped with exception: {ex}");
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

        Debug.WriteLine($"TemperaturePollingService synchronized with sampleRate: {sampleRateInSeconds} seconds");
    }
    private async Task DoWorkAsync(CancellationToken token)
    {
        try
        {
            await CheckForNewTemperatures().ConfigureAwait(false);

            while (_timer != null && await _timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                Debug.WriteLine("TemperaturePollingService triggered");
                await CheckForNewTemperatures().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("TemperaturePollingService cancelled");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TemperaturePollingService stopped unexpected: {ex}");
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
            // get JSON payload deserialized to EspLogDto
            var dto = await _esp32Service.GetTemperatureLogFromESP().ConfigureAwait(false);
            if (dto == null || dto.Days == null || !dto.Days.Any())
            {
                // nothing new
                MainPage.Current.DispatcherQueue.TryEnqueue(() =>
                {
                    _messenger.Send(new GraphUpdatedMessage());
                });
                return true;
            }

            // ensure archive folder exists
            var archiveFolder = Path.Combine(AppConstants.AppDataPath, "DailyArchive");
            Directory.CreateDirectory(archiveFolder);

            // collect all data points across archived days (after merge)
            var allTemperaturesOnMultipleDays = new List<DataPoint>();

            foreach (var day in dto.Days)
            {
                // normalize date string (expect "yyyy-MM-dd" but fallback to parseable format)
                var dateStr = day.Date;
                DateTime parsedDate;
                if (!DateTime.TryParse(dateStr, out parsedDate))
                {
                    // try extracting only first 10 chars if contained time
                    if (dateStr?.Length >= 10)
                        DateTime.TryParse(dateStr[..10], out parsedDate);
                    else
                        parsedDate = DateTime.Now.Date;
                }

                var fileName = Path.Combine(archiveFolder, $"{parsedDate:yyyy-MM-dd}.json");

                // merge existing archive (if any) with incoming records by unique ts
                DayDto mergedDay;
                if (File.Exists(fileName))
                {
                    try
                    {
                        var existingJson = await File.ReadAllTextAsync(fileName).ConfigureAwait(false);
                        var existing = JsonConvert.DeserializeObject<DayDto>(existingJson) ?? new DayDto { Date = parsedDate.ToString("yyyy-MM-dd"), Records = new List<RecordDto>() };
                        // combine and deduplicate by ts (keep latest entry from incoming when duplicate)
                        var dict = new Dictionary<long, RecordDto>();
                        foreach (var r in existing.Records ?? Enumerable.Empty<RecordDto>())
                            dict[r.Ts] = r;
                        foreach (var r in day.Records ?? Enumerable.Empty<RecordDto>())
                            dict[r.Ts] = r; // incoming overwrites existing
                        var mergedRecords = dict.Values.OrderBy(r => r.Ts).ToList();
                        mergedDay = new DayDto
                        {
                            Date = parsedDate.ToString("yyyy-MM-dd"),
                            Count = mergedRecords.Count,
                            Records = mergedRecords
                        };
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to read/merge existing archive '{fileName}': {ex}");
                        // fallback to use incoming day as-is
                        mergedDay = new DayDto
                        {
                            Date = parsedDate.ToString("yyyy-MM-dd"),
                            Count = day.Records?.Count ?? 0,
                            Records = day.Records?.OrderBy(r => r.Ts).ToList() ?? new List<RecordDto>()
                        };
                    }
                }
                else
                {
                    mergedDay = new DayDto
                    {
                        Date = parsedDate.ToString("yyyy-MM-dd"),
                        Count = day.Records?.Count ?? 0,
                        Records = day.Records?.OrderBy(r => r.Ts).ToList() ?? new List<RecordDto>()
                    };
                }

                // write merged day to disk (pretty printed)
                try
                {
                    var outJson = JsonConvert.SerializeObject(mergedDay, Formatting.Indented);
                    await File.WriteAllTextAsync(fileName, outJson).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to write archive file '{fileName}': {ex}");
                }

                // convert merged records to DataPoint for graph service (use local time)
                foreach (var rec in mergedDay.Records ?? Enumerable.Empty<RecordDto>())
                {
                    try
                    {
                        var dtoTime = DateTimeOffset.FromUnixTimeSeconds(rec.Ts).LocalDateTime;
                        allTemperaturesOnMultipleDays.Add(new DataPoint { Timestamp = dtoTime, Value = rec.Temp });
                    }
                    catch { /* ignore invalid ts */ }
                }
            }

            // notify graph service with today's temperatures
            var today = DateOnly.FromDateTime(DateTime.Now);
            var todaysTemperatures = allTemperaturesOnMultipleDays
                .Where(p => DateOnly.FromDateTime(p.Timestamp) == today)
                .OrderBy(p => p.Timestamp)
                .ToList();

           // _graphService.AppendTemperaturesToCurrentDay(todaysTemperatures);
            var tempLogger = _temperatureLoggerStore.GetOrCreate(dto.Device);
            tempLogger.AddCurrentReadings(todaysTemperatures);

            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
            {
                _messenger.Send(new GraphUpdatedMessage());
            });

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("CheckForNewGraphData failed: " + ex);
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

