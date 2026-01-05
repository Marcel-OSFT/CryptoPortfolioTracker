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
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using TemperatureMonitor;
using TemperatureMonitor.Models;
using TemperatureMonitor.ViewModels;
using Windows.Media.Protection.PlayReady;
using Task = System.Threading.Tasks.Task;

namespace TemperatureMonitor.Services;

public class Esp32Service : IDisposable
{
    string json = string.Empty;

    private readonly Settings _appSettings;
    private readonly IGraphService _graphService;
    private readonly IMessenger _messenger;
    private readonly ITemperatureLoggerStore _temperatureLoggerStore;
    private string EspIpAddress = string.Empty;

    // Replace simple bool locker with AsyncLock to support timeout and cancellation
    private readonly AsyncLock _asyncLock = new AsyncLock();

    // Default timeout to acquire the lock (adjust as needed)
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    // single HttpClient instance for this service (reused to avoid socket exhaustion)
    private readonly HttpClient _httpClient;

    public Esp32Service(IGraphService graphService,ITemperatureLoggerStore temperatureLoggerStore ,IMessenger messenger, Settings appSettings)
    {
        _appSettings = appSettings;
        _graphService = graphService;
        _messenger = messenger;
        _temperatureLoggerStore = temperatureLoggerStore;

        // Initialize single HttpClient instance with an appropriate timeout
        _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    // Helper to get string content with cancellation and proper status checking
    private async Task<string> GetStringContentAsync(string requestUri, CancellationToken ct = default)
    {
        using var resp = await _httpClient.GetAsync(requestUri, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
    private async Task<string> GetJsonContentAsync(string requestUri, CancellationToken ct = default)
    {
        var json = await _httpClient.GetStringAsync(requestUri);
        return json;
    }

    public async Task<EspLogDto> GetTemperatureLogFromESP(CancellationToken cancellationToken = default)
    {
        EspLogDto dto = new();
        var releaser = await _asyncLock.LockAsync(LockTimeout, cancellationToken).ConfigureAwait(false);
        if (!releaser.IsAcquired)
        {
            Debug.WriteLine("GetTemperatureLogFromESP: failed to acquire lock within timeout.");
            return dto;
        }

        using (releaser)
        {
            bool usedManualIp = false;
            string ipToUse = string.Empty;
            string response = string.Empty;
            try
            {
                const int scanMilliseconds = 1500; // per-attempt scan window
                var discovered = (List<DeviceInfo>?)await DiscoveryService.DiscoverOnceAsync(scanMilliseconds: scanMilliseconds, cancellationToken: default).ConfigureAwait(false);
                // first build app for one device.
                var espDevice = discovered?.FirstOrDefault();
                ipToUse = espDevice?.IP ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ipToUse))
                {
                    return dto;
                }
                // Handshake with ESP device to inform that communication is starting (this ensures that the WiFi ON window of the device is extended for the communication duration
                response = await GetStringContentAsync($"http://{ipToUse}/session/start", cancellationToken).ConfigureAwait(false);
                Debug.WriteLine($"{response}");

                Debug.WriteLine($"GetTemperatureLogFromESP: downloading log from http://{ipToUse}/logs.json");

                //string logData = await GetStringContentAsync($"http://{ipToUse}/logs", cancellationToken).ConfigureAwait(false);
                var json = await GetJsonContentAsync($"http://{ipToUse}/logs.json", cancellationToken).ConfigureAwait(false);
                dto = EspLogDto.FromJson(json);
                
                // cache logger instance
                var TempLogger = _temperatureLoggerStore.GetOrCreate(dto.Device);
                TempLogger?.IpAddress = ipToUse;
                // Clear log file on ESP
                var result = dto != null && dto.Days.Any();
                if (result)
                {
                    Debug.WriteLine($"GetTemperatureLogFromESP: downloaded log with {dto.Days.Sum(d => d.Count)} records over {dto.Days.Count} days.");
                    // clear the logs on the ESP device
                    response = await GetStringContentAsync($"http://{ipToUse}/clearlogs", cancellationToken).ConfigureAwait(false);
                    Debug.WriteLine($"Clearing logs on ESP: {response}");
                }
                else
                {
                    Debug.WriteLine("GetTemperatureLogFromESP: downloaded log is empty.");
                }

                // Handshake with ESP device to inform that communication is finsihed (this enables the device to go into early sleep)
                response = await GetStringContentAsync($"http://{ipToUse}/session/end", cancellationToken).ConfigureAwait(false);
                Debug.WriteLine($"{response}");

                return dto;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("GetTemperatureLogFromESP cancelled.");
                return dto;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetTemperatureLogFromESP failed: {ex}");
                // if we used a discovered IP, clear it so next call will rediscover
                if (!usedManualIp) EspIpAddress = string.Empty;
                return dto;
            }
        }
    }

    public async Task<bool> SetSampleRate(int sampleRateInSeconds, CancellationToken cancellationToken = default)
    {
        var releaser = await _asyncLock.LockAsync(LockTimeout, cancellationToken).ConfigureAwait(false);
        if (!releaser.IsAcquired)
        {
            Debug.WriteLine("SetSampleRate: failed to acquire lock within timeout.");
            return false;
        }

        using (releaser)
        {
            bool usedManualIp = false;
            string ipToUse = string.Empty;
            try
            {
                const int scanMilliseconds = 1500; // per-attempt scan window
                var discovered = (List<DeviceInfo>?)await DiscoveryService.DiscoverOnceAsync(scanMilliseconds: scanMilliseconds, cancellationToken: default).ConfigureAwait(false);
                var espDevice = discovered?.FirstOrDefault();
                ipToUse = espDevice?.IP ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ipToUse))
                {
                    return false;
                }

                string url = $"http://{ipToUse}/setsample?sec={sampleRateInSeconds}";
                Debug.WriteLine($"SetSampleRate: sending request to {url}");

                string response = await GetStringContentAsync(url, cancellationToken).ConfigureAwait(false);

                Debug.WriteLine($"Setting Sample Interval on ESP: {response}");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("SetSampleRate cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SetSampleRateOnESP failed: {ex}");
                if (!usedManualIp) EspIpAddress = string.Empty;
                return false;
            }
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

    public void Dispose()
    {
        _asyncLock.Dispose();
        _httpClient.Dispose();
    }
}