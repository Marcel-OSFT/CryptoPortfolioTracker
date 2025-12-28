using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TemperatureMonitor.Models;

namespace TemperatureMonitor.Services
{
    /// <summary>
    /// Reads a log file where each line is: "unix_seconds;temperature"
    /// Example: "1672444800;25.34"
    /// Returns a List<DataPoint> where Timestamp is UTC DateTime.
    /// Malformed lines are skipped.
    /// </summary>
    public static class MockLogReader
    {
        public static async Task<List<DataPoint>> ReadMockLogFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("filePath is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Log file not found.", filePath);

            var list = new List<DataPoint>();

            using var sr = new StreamReader(filePath, Encoding.UTF8);
            string? line;
            while ((line = await sr.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(';');
                if (parts.Length < 2) continue;

                var tsPart = parts[0].Trim();
                var valPart = parts[1].Trim();

                if (!long.TryParse(tsPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
                    continue;

                if (!double.TryParse(valPart, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value))
                    continue;

                // create UTC DateTime from unix seconds reliably
                var dateTimeUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;

                list.Add(new DataPoint { Timestamp = dateTimeUtc, Value = value });
            }

            return list;
        }
    }
}