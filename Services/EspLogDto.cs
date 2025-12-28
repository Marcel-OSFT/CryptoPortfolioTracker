using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TemperatureMonitor.Services
{
    // Root DTO for the ESP JSON payload
    public class EspLogDto
    {
        [JsonProperty("device")]
        public string Device { get; set; } = string.Empty;

        [JsonProperty("days")]
        public List<DayDto> Days { get; set; } = new();

        public static EspLogDto FromJson(string json) =>
            JsonConvert.DeserializeObject<EspLogDto>(json)
            ?? throw new JsonException("Failed to deserialize EspLogDto");
    }

    // DTO for a single day block
    public class DayDto
    {
        // original "2025-01-18" string from payload
        [JsonProperty("date")]
        public string Date { get; set; } = string.Empty;

        // integer count
        [JsonProperty("count")]
        public int Count { get; set; }

        // list of measurements for that day
        [JsonProperty("records")]
        public List<RecordDto> Records { get; set; } = new();

        // Convenience: parse `Date` into DateOnly (safe on .NET 6)
        [JsonIgnore]
        public DateOnly? DateOnly
        {
            get
            {
                if (DateTime.TryParse(Date, out var dt))
                    return DateOnly.FromDateTime(dt);
                return null;
            }
        }
    }

    // DTO for a single timestamped temperature record
    public class RecordDto
    {
        // epoch seconds
        [JsonProperty("ts")]
        public long Ts { get; set; }

        // temperature value
        [JsonProperty("temp")]
        public double Temp { get; set; }

        // Convenience: convert epoch seconds to UTC DateTimeOffset
        [JsonIgnore]
        public DateTimeOffset TimestampUtc => DateTimeOffset.FromUnixTimeSeconds(Ts);

        // Convenience: local DateTime
        [JsonIgnore]
        public DateTime TimestampLocal => TimestampUtc.LocalDateTime;
    }
}
