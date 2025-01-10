
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IGraphUpdateService
    {
        public bool IsUpdating { get; }
        public void Start();
        public Task Stop();
        public void Pause();
        public Task Resume();
    }
}
