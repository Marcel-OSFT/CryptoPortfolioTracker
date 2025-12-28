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
    private string EspIpAddress = string.Empty;

    // Replace simple bool locker with AsyncLock to support timeout and cancellation
    private readonly AsyncLock _asyncLock = new AsyncLock();

    // Default timeout to acquire the lock (adjust as needed)
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    // single HttpClient instance for this service (reused to avoid socket exhaustion)
    private readonly HttpClient _httpClient;

    public Esp32Service(IGraphService graphService, IMessenger messenger, Settings appSettings)
    {
        _appSettings = appSettings;
        _graphService = graphService;
        _messenger = messenger;

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

       // using var resp = await _httpClient.GetAsync(requestUri, ct).ConfigureAwait(false);
        //resp.EnsureSuccessStatusCode();
        return json;
    }


    



    // Centralized resolver for ESP IP (prefers manual setting, falls back to discovery)
    // If showMessageIfNotFound is true and no IP is found, a user dialog will be enqueued.
    private async Task<(bool usedManualIp, string ip)> ResolveEspIpAsync(CancellationToken ct = default, bool showMessageIfNotFound = true)
    {
        // prefer manual configured IP
        if (!string.IsNullOrWhiteSpace(_appSettings.EspIpAddress))
        {
            Debug.WriteLine($"ResolveEspIpAsync: using manual IP from settings: '{_appSettings.EspIpAddress}'");
            return (true, _appSettings.EspIpAddress);
        }

        // use cached discovered IP if present
        if (!string.IsNullOrWhiteSpace(EspIpAddress))
        {
            Debug.WriteLine($"ResolveEspIpAsync: using cached discovered IP: '{EspIpAddress}'");
            return (false, EspIpAddress);
        }

        const int attempts = 3;
        const int scanMilliseconds = 1500; // per-attempt scan window
        const int delayBetweenMs = 500;     // small backoff between attempts

        List<DeviceInfo>? lastDiscovered = null;

        for (int attempt = 1; attempt <= attempts && !ct.IsCancellationRequested; attempt++)
        {
            try
            {
                Debug.WriteLine($"ResolveEspIpAsync: discovery attempt {attempt}/{attempts} (scan {scanMilliseconds}ms)");
                var discovered = (List<DeviceInfo>?)await DiscoveryService.DiscoverOnceAsync(
                    scanMilliseconds: scanMilliseconds,
                    cancellationToken: ct).ConfigureAwait(false);

                lastDiscovered = discovered?.ToList();

                var espDevice = discovered?.FirstOrDefault();
                EspIpAddress = espDevice?.IP ?? string.Empty;

                Debug.WriteLine($"ResolveEspIpAsync: discovered devices: {(discovered == null ? "none" : discovered.Count.ToString())}");
                Debug.WriteLine($"ResolveEspIpAsync: EspIpAddress set to: '{EspIpAddress}'");

                if (!string.IsNullOrWhiteSpace(EspIpAddress))
                {
                    return (false, EspIpAddress);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("ResolveEspIpAsync cancelled.");
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ResolveEspIpAsync attempt {attempt} failed: {ex}");
                // clear any potentially stale cache
                EspIpAddress = string.Empty;
            }

            if (attempt < attempts)
            {
                try { await Task.Delay(delayBetweenMs, ct).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
            }
        }

        // nothing found via Zeroconf; optionally run a lightweight subnet scan to list responsive hosts
        List<DeviceInfo>? responsiveHosts = null;
        try
        {
            // perform a conservative scan (ScanLocalSubnetAsync limits to /24 and is parallel)
            responsiveHosts = await NetworkScanner.ScanLocalSubnetAsync(pingTimeoutMs: 350, maxDegreeOfParallelism: 80, ct: ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("ResolveEspIpAsync: network scan cancelled.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ResolveEspIpAsync: network scan failed: {ex}");
        }

        Debug.WriteLine("ResolveEspIpAsync: no ESP IP available after discovery attempts.");
        if (showMessageIfNotFound && MainPage.Current != null)
        {
            // build message including discovered service devices and responsive hosts if any
            string message;
            var parts = new List<string>
            {
                "No ESP32 discovered on the network."
            };

            if (lastDiscovered != null && lastDiscovered.Count > 0)
            {
                parts.Add("");
                parts.Add("Devices advertising the temperature service:");
                parts.AddRange(lastDiscovered.Select(d => $"- {d.IP} ({d.Name ?? "unknown"})"));
            }

            if (responsiveHosts != null && responsiveHosts.Count > 0)
            {
                parts.Add("");
                parts.Add("Other responsive hosts on your subnet:");
                // limit list length to avoid overly long dialogs
                foreach (var d in responsiveHosts.Take(50))
                    parts.Add($"- {d.IP} ({d.Name ?? "unknown"})");
                if (responsiveHosts.Count > 50)
                    parts.Add($"... and {responsiveHosts.Count - 50} more");
            }

            parts.Add("");
            parts.Add("If your ESP is not listed, enter its IP address manually in Settings -> ESP32 — Manual IP.");

            message = string.Join(Environment.NewLine, parts);

            MainPage.Current.DispatcherQueue.TryEnqueue(() =>
                _ = ShowMessageDialog("ESP not found", message, "OK"));
        }

        return (false, string.Empty);
    }

    public async Task<bool> GetTemperatureLogFromESP(CancellationToken cancellationToken = default)
    {
        var releaser = await _asyncLock.LockAsync(LockTimeout, cancellationToken).ConfigureAwait(false);
        if (!releaser.IsAcquired)
        {
            Debug.WriteLine("GetTemperatureLogFromESP: failed to acquire lock within timeout.");
            return false;
        }

        using (releaser)
        {
            bool usedManualIp = false;
            string ipToUse = string.Empty;
            try
            {
                // use cached discovered IP if present
                if (!string.IsNullOrWhiteSpace(EspIpAddress))
                {
                    ipToUse = EspIpAddress;
                }
                else
                {
                    var resolved = await ResolveEspIpAsync(cancellationToken, showMessageIfNotFound: true).ConfigureAwait(false);
                    usedManualIp = resolved.usedManualIp;
                    ipToUse = resolved.ip;
                }


                if (string.IsNullOrWhiteSpace(ipToUse))
                {
                    return false;
                }

                Debug.WriteLine($"GetTemperatureLogFromESP: downloading log from http://{ipToUse}/logs.json");

                //string logData = await GetStringContentAsync($"http://{ipToUse}/logs", cancellationToken).ConfigureAwait(false);
                var json = await GetJsonContentAsync($"http://{ipToUse}/logs.json", cancellationToken).ConfigureAwait(false);
                var dto = EspLogDto.FromJson(json);


                // Save to file
                string path = Path.Combine(AppConstants.AppDataPath, "TemperatureLog.txt");
                await File.WriteAllTextAsync(path, logData, cancellationToken).ConfigureAwait(false);

                Debug.WriteLine($"Saved log to {path}");

                // Clear log file on ESP
                var result = !string.IsNullOrEmpty(logData);
                if (!result)
                {
                    Debug.WriteLine("GetTemperatureLogFromESP: downloaded log is empty.");
                    // only clear discovered cache (not user-provided setting)
                    //if (!usedManualIp) EspIpAddress = string.Empty;
                    return false;
                }

                string response = await GetStringContentAsync($"http://{ipToUse}/clearlogs", cancellationToken).ConfigureAwait(false);
                Debug.WriteLine($"Clearing log on ESP: {response}");
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("GetTemperatureLogFromESP cancelled.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetTemperatureLogFromESP failed: {ex}");
                // if we used a discovered IP, clear it so next call will rediscover
                if (!usedManualIp) EspIpAddress = string.Empty;
                return false;
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
                // use cached discovered IP if present
                if (!string.IsNullOrWhiteSpace(EspIpAddress))
                {
                    ipToUse = EspIpAddress;
                }
                else
                {
                    var resolved = await ResolveEspIpAsync(cancellationToken, showMessageIfNotFound: true).ConfigureAwait(false);
                    usedManualIp = resolved.usedManualIp;
                    ipToUse = resolved.ip;
                }

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