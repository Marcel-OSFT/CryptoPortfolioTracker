using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace TemperatureMonitor.Services
{
    /// <summary>
    /// Creates a mock temperature log file.
    /// Default: 3 days, 1 minute interval, temperatures between 25.0 and 30.0 °C.
    /// Each line format: "unix_timestamp;25.34" (unix timestamp in seconds, UTC)
    /// </summary>
    public static class MockLogGenerator
    {
        public static async Task CreateMockLogFileAsync(
            string filePath,
            int days = 3,
            TimeSpan? interval = null,
            double minTemp = 25.0,
            double maxTemp = 30.0,
            int decimals = 2,
            int? randomSeed = null)
        {
            if (interval == null) interval = TimeSpan.FromMinutes(1);
            if (minTemp > maxTemp) throw new ArgumentException("minTemp must be <= maxTemp.");
            if (days <= 0) throw new ArgumentException("days must be > 0.");

            var rng = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            var end = DateTime.UtcNow;
            var start = end.AddDays(-days);
            var stepSeconds = interval.Value.TotalSeconds;
            if (stepSeconds <= 0) throw new ArgumentException("interval must be positive.");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? ".");

            // Use UTF8 without BOM for compatibility with many ESP log readers
            await using var sw = new StreamWriter(filePath, false, new UTF8Encoding(false));

            // Generate readings: iterate from start to end using the provided interval
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var ts = start; ts <= end; ts = ts.AddSeconds(stepSeconds))
            {
                // simple uniform random with a slight diurnal drift for realism
                var baseTemp = minTemp + rng.NextDouble() * (maxTemp - minTemp);
                var diurnal = Math.Sin(2 * Math.PI * (ts.TimeOfDay.TotalHours / 24.0)) * 0.5; // ±0.5°C
                var temp = baseTemp + diurnal;

                var unixSeconds = (long)(ts - epoch).TotalSeconds;
                var tempStr = Math.Round(temp, decimals).ToString($"F{decimals}", CultureInfo.InvariantCulture);
                var line = $"{unixSeconds};{tempStr}";
                await sw.WriteLineAsync(line);
            }
        }
    }
}