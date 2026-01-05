
using System.Threading.Tasks;

namespace TemperatureMonitor.Services
{
    public interface ITemperaturePollingService
    {
        public bool IsUpdating { get; }
        public Task StartAsync();
        public Task StopAsync();
       
    }
}
