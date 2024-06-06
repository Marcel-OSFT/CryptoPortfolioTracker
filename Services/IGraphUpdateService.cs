
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IGraphUpdateService
    {
        public Task Start();
        public Task Stop();
        public void Pause();
        public void Continue();
    }
}
