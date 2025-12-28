using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TemperatureMonitor.Models;

namespace TemperatureMonitor.Services
{
    /// <summary>
    /// Archives temperature readings into one JSON file per calendar day.
    /// - Files are stored under: {baseFolder}\DailyArchive\yyyy-MM-dd.json
    /// - Incoming readings overwrite existing readings with the same Timestamp (UTC).
    /// - Default grouping is by local calendar date. Set useLocalDate = false to group by UTC date.
    /// </summary>
    public static class DailyArchiveService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Archive a collection of data points into daily JSON files.
        /// </summary>
        /// <param name="points">Collection of DataPoint (Timestamp in UTC recommended)</param>
        /// <param name="baseFolder">Root folder (e.g. AppConstants.AppDataPath). If null/empty uses current directory.</param>
        /// <param name="useLocalDate">If true group by local calendar day; otherwise group by UTC date.</param>
        public static async Task ArchiveAsync(IEnumerable<DataPoint> points, string? baseFolder = null, bool useLocalDate = true, CancellationToken ct = default)
        {
            if (points is null) return;

            var root = string.IsNullOrWhiteSpace(baseFolder) ? Directory.GetCurrentDirectory() : baseFolder!;
            var archiveFolder = Path.Combine(root, "DailyArchive");
            Directory.CreateDirectory(archiveFolder);

            // group by calendar day
            var groups = points
                .Where(p => p != null)
                .GroupBy(p => GetGroupDate(p.Timestamp, useLocalDate));

            foreach (var grp in groups)
            {
                ct.ThrowIfCancellationRequested();
                var date = grp.Key;
                var incoming = grp.ToList();

                var filePath = GetDayFilePath(archiveFolder, date);

                var existing = await ReadDayFileAsync(filePath, ct).ConfigureAwait(false);

                var merged = MergeOverwriteByTimestamp(existing, incoming);

                await WriteDayFileAtomicAsync(filePath, merged, ct).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Load a single day's archived points (returns empty list if file missing or invalid).
        /// </summary>
        public static async Task<List<DataPoint>> LoadDayAsync(DateOnly date, string? baseFolder = null, bool useLocalDate = true, CancellationToken ct = default)
        {
            var root = string.IsNullOrWhiteSpace(baseFolder) ? Directory.GetCurrentDirectory() : baseFolder!;
            var archiveFolder = Path.Combine(root, "DailyArchive");
            var filePath = GetDayFilePath(archiveFolder, date);
            return await ReadDayFileAsync(filePath, ct).ConfigureAwait(false);
        }

        private static DateOnly GetGroupDate(DateTime timestamp, bool useLocalDate)
        {
            return useLocalDate
                ? DateOnly.FromDateTime(timestamp.ToLocalTime())
                : DateOnly.FromDateTime(timestamp.ToUniversalTime());
        }

        private static string GetDayFilePath(string archiveFolder, DateOnly date)
            => Path.Combine(archiveFolder, date.ToString("yyyy-MM-dd") + ".json");

        private static async Task<List<DataPoint>> ReadDayFileAsync(string filePath, CancellationToken ct)
        {
            try
            {
                if (!File.Exists(filePath)) return new List<DataPoint>();

                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var list = await JsonSerializer.DeserializeAsync<List<DataPoint>>(fs, _jsonOptions, ct).ConfigureAwait(false);
                return list ?? new List<DataPoint>();
            }
            catch
            {
                // If file is malformed or cannot be read, return empty list so we can overwrite it safely.
                return new List<DataPoint>();
            }
        }

        private static List<DataPoint> MergeOverwriteByTimestamp(List<DataPoint> existing, List<DataPoint> incoming)
        {
            // Use UTC ticks to identify unique timestamps robustly
            var dict = new Dictionary<long, DataPoint>();

            if (existing != null)
            {
                foreach (var dp in existing)
                {
                    if (dp == null) continue;
                    var key = dp.Timestamp.ToUniversalTime().Ticks;
                    dict[key] = dp;
                }
            }

            foreach (var dp in incoming)
            {
                if (dp == null) continue;
                var key = dp.Timestamp.ToUniversalTime().Ticks;
                dict[key] = dp; // overwrite existing with incoming
            }

            var merged = dict.Values
                .OrderBy(d => d.Timestamp.ToUniversalTime())
                .ToList();

            return merged;
        }

        private static async Task WriteDayFileAtomicAsync(string filePath, List<DataPoint> data, CancellationToken ct)
        {
            // Ensure folder exists
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tempPath = filePath + ".tmp";
            // write to temp file then move to final path to minimize risk of corrupt files
            await using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await JsonSerializer.SerializeAsync(fs, data, _jsonOptions, ct).ConfigureAwait(false);
                await fs.FlushAsync(ct).ConfigureAwait(false);
            }

            // Replace (overwrite) the target file atomically where supported
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.Move(tempPath, filePath);
        }
    }
}