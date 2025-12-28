using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace TemperatureMonitor.Services
{
    // Re-uses DeviceInfo defined in DiscoveryService.cs
    public static class NetworkScanner
    {
        public static async Task<List<DeviceInfo>> ScanLocalSubnetAsync(
            int pingTimeoutMs = 350,
            int maxDegreeOfParallelism = 100,
            CancellationToken ct = default)
        {
            var (localIp, mask) = GetLocalIPv4AndMask();
            if (localIp == null) return new List<DeviceInfo>();

            uint ipUint = ToUint(localIp);
            uint maskUint = ToUint(mask);
            uint network = ipUint & maskUint;
            uint broadcast = network | ~maskUint;

            // avoid scanning huge subnets; limit to /16 or smaller by truncating range
            uint maxHosts = broadcast - network - 1;
            if (maxHosts > 1024)
            {
                // clip to /24 range around local IP for safety (common home networks)
                uint baseNet = (ipUint & 0xFFFFFF00u);
                network = baseNet;
                broadcast = baseNet | 0xFFu;
            }

            var results = new List<DeviceInfo>();
            var sem = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            for (uint candidate = network + 1; candidate < broadcast; candidate++)
            {
                if (ct.IsCancellationRequested) break;

                var ipAddr = FromUint(candidate);
                var ipString = ipAddr.ToString();

                await sem.WaitAsync(ct).ConfigureAwait(false);
                var t = Task.Run(async () =>
                {
                    try
                    {
                        using var ping = new Ping();
                        var reply = await ping.SendPingAsync(ipString, pingTimeoutMs).ConfigureAwait(false);
                        if (reply.Status == IPStatus.Success)
                        {
                            string hostName = ipString;
                            try
                            {
                                // reverse DNS may hang; keep it optional and non-fatal
                                var dnsTask = Dns.GetHostEntryAsync(ipString);
                                var completed = await Task.WhenAny(dnsTask, Task.Delay(500, ct)).ConfigureAwait(false);
                                if (completed == dnsTask)
                                {
                                    var entry = await dnsTask.ConfigureAwait(false);
                                    if (!string.IsNullOrWhiteSpace(entry.HostName))
                                        hostName = entry.HostName;
                                }
                            }
                            catch { /* ignore reverse lookup failures */ }

                            lock (results)
                            {
                                results.Add(new DeviceInfo { IP = ipString, Name = hostName, Hostname = hostName });
                            }
                        }
                    }
                    catch { /* ignore per-host exceptions */ }
                    finally
                    {
                        sem.Release();
                    }
                }, ct);

                tasks.Add(t);
            }

            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            // sort by IP
            return results.OrderBy(d => d.IP).ToList();
        }

        private static (IPAddress? ip, IPAddress? mask) GetLocalIPv4AndMask()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                var props = ni.GetIPProperties();
                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var mask = ua.IPv4Mask ?? IPAddress.Parse("255.255.255.0");
                        return (ua.Address, mask);
                    }
                }
            }

            // fallback: try DNS host entries
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var addr = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                if (addr != null) return (addr, IPAddress.Parse("255.255.255.0"));
            }
            catch { }

            return (null, null);
        }

        private static uint ToUint(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static IPAddress FromUint(uint val)
        {
            var bytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return new IPAddress(bytes);
        }
    }
}