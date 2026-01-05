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
        public static async Task<IReadOnlyList<DeviceInfo>> DiscoverOnceAsync(string serviceType = "_temperature._tcp.local.", int scanMilliseconds = 3000, CancellationToken cancellationToken = default)
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

    }

    public class DeviceInfo
    {
        public string Name { get; set; }
        public string Hostname { get; set; }
        public string IP { get; set; }
    }
}