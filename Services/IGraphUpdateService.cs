
using System.Threading.Tasks;

namespace TemperatureMonitor.Services
{
    public interface IGraphUpdateService
    {
        public bool IsUpdating { get; }
        public Task StartAsync();
        public Task StopAsync();
       
    }
}
