using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace TemperatureMonitor.Models
{
    /// <summary>
    /// Lightweight container for a device and its temperature readings.
    /// </summary>
    public class TemperatureLogger
    {
        private readonly object _sync = new();

        // Device identity
        public string DeviceId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;

        // Readings for the current day (local timestamps)
        public List<DataPoint> CurrentDayReadings { get; } = new();

        // Readings collected while CurrentMode == Now (short lived)
        public List<DataPoint> NowModeReadings { get; } = new();

        public TemperatureLogger() { }

        public TemperatureLogger(string deviceId, string ipAddress = "", string alias = "")
        {
            DeviceId = deviceId;
            IpAddress = ipAddress;
            Alias = alias;
        }

        // Add a single reading to NowMode collection (thread-safe)
        public void AddNowModeReading(DataPoint point)
        {
            if (point == null) return;
            lock (_sync)
            {
                NowModeReadings.Add(point);
                NowModeReadings.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            }
        }

        // Add one or more readings to current day
        public void AddCurrentReadings(IEnumerable<DataPoint> points)
        {
            if (points == null) return;
            var pts = points.Where(p => p != null).ToList();
            if (!pts.Any()) return;

            lock (_sync)
            {
                CurrentDayReadings.AddRange(pts);
                CurrentDayReadings.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            }
        }

        /// <summary>
        /// Read archived JSON for the specified calendar day and return a list of DataPoint.
        /// Expects archive files at "{appDataPath}/DailyArchive/yyyy-MM-dd.json" in the DayDto format:
        /// { "date": "...", "count": n, "records": [ { "ts": 12345, "temp": 23.4 }, ... ] }
        /// </summary>
        public List<DataPoint> GetArchivedReadings(DateOnly date, string appDataPath)
        {
            var points = new List<DataPoint>();

            if (string.IsNullOrWhiteSpace(appDataPath)) return points;

            var archiveFolder = Path.Combine(appDataPath, "DailyArchive");
            var fileName = Path.Combine(archiveFolder, $"{date:yyyy-MM-dd}.json");

            if (!File.Exists(fileName))
                return points;

            try
            {
                var json = File.ReadAllText(fileName);
                // DayDto is defined in TemperatureMonitor.Services namespace
                var day = JsonConvert.DeserializeObject<TemperatureMonitor.Services.DayDto>(json);
                if (day?.Records == null) return points;

                foreach (var r in day.Records)
                {
                    if (r == null) continue;
                    try
                    {
                        var dtoTime = DateTimeOffset.FromUnixTimeSeconds(r.Ts).LocalDateTime;
                        points.Add(new DataPoint { Timestamp = dtoTime, Value = r.Temp });
                    }
                    catch
                    {
                        // ignore invalid timestamp entries
                    }
                }
            }
            catch
            {
                // If anything goes wrong, return empty list (caller can decide how to handle)
            }

            return points.OrderBy(p => p.Timestamp.ToUniversalTime()).ToList();
        }

        public void ClearNowModeReadings()
        {
            lock (_sync) NowModeReadings.Clear();
        }

        public void ClearCurrentDayReadings()
        {
            lock (_sync) CurrentDayReadings.Clear();
        }
    }
}