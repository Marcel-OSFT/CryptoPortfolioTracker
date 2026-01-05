using System.Collections.Generic;

namespace TemperatureMonitor.Models
{
    public interface ITemperatureLoggerStore
    {
        TemperatureLogger GetOrCreate(string deviceId);
        bool TryGet(string deviceId, out TemperatureLogger logger);
        IEnumerable<TemperatureLogger> GetAll();
        bool Remove(string deviceId);
        void Clear();
    }
}