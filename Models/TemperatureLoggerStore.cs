using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TemperatureMonitor.Models
{
    public class TemperatureLoggerStore : ITemperatureLoggerStore
    {
        private readonly ConcurrentDictionary<string, TemperatureLogger> _map = new();

        public TemperatureLogger GetOrCreate(string deviceId) =>
            _map.GetOrAdd(deviceId ?? string.Empty, id => new TemperatureLogger(id));

        public bool TryGet(string deviceId, out TemperatureLogger logger) =>
            _map.TryGetValue(deviceId ?? string.Empty, out logger);

        public IEnumerable<TemperatureLogger> GetAll() => _map.Values;

        public bool Remove(string deviceId) => _map.TryRemove(deviceId ?? string.Empty, out _);

        public void Clear() => _map.Clear();
    }
}