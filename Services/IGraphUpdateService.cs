
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IGraphUpdateService
    {
        public bool IsUpdating { get; }
        public void Start();
        public void Stop();
        public void Pause(bool isDisconnecting = false);
        public void Resume();
    }
}
