using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;

namespace TemperatureMonitor.Services
{
    public static class DiscoveryService
    {
        // Discover once and return found devices
        public static async Task<IReadOnlyList<DeviceInfo>> DiscoverOnceAsync(
            string serviceType = "_temperature._tcp.local.",
            int scanMilliseconds = 3000,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Requires NuGet package: Zeroconf
                // dotnet add package Zeroconf
                var scanTime = TimeSpan.FromMilliseconds(scanMilliseconds);
                var hosts = await ZeroconfResolver.ResolveAsync(serviceType, scanTime, cancellationToken: cancellationToken);

                return hosts.Select(h => new DeviceInfo
                {
                    Name = h.DisplayName ?? h.Id,
                    Hostname = h.Id,
                    IP = h.IPAddress
                }).ToList();


            }
            catch (Exception ex)
            {
               Debug.WriteLine("DiscoveryService failed: " + ex.Message);
            }
            return Array.Empty<DeviceInfo>();

        }

        // Optional: continuously watch and call the callback when changes occur
        public static async Task StartWatchingAsync(
            Action<IReadOnlyList<DeviceInfo>> onUpdate,
            string serviceType = "_temperature._tcp.local.",
            int intervalMilliseconds = 5000,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<DeviceInfo> previous = Array.Empty<DeviceInfo>();

            while (!cancellationToken.IsCancellationRequested)
            {
                var current = await DiscoverOnceAsync(serviceType, scanMilliseconds: Math.Min(intervalMilliseconds, 5000), cancellationToken: cancellationToken);

                // Simple change detection
                if (!AreEqual(previous, current))
                {
                    previous = current;
                    onUpdate?.Invoke(current);
                }

                try
                {
                    await Task.Delay(intervalMilliseconds, cancellationToken);
                }
                catch (OperationCanceledException) { break; }
            }
        }

        static bool AreEqual(IReadOnlyList<DeviceInfo> a, IReadOnlyList<DeviceInfo> b)
        {
            if (a.Count != b.Count) return false;
            var sa = new HashSet<string>(a.Select(d => d.IP + "|" + d.Name));
            var sb = new HashSet<string>(b.Select(d => d.IP + "|" + d.Name));
            return sa.SetEquals(sb);
        }
    }

    public class DeviceInfo
    {
        public string Name { get; set; }
        public string Hostname { get; set; }
        public string IP { get; set; }
    }
}