using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IPriceUpdateService
{
    public void Start();
    public void Pause();
    public void Continue();
    public Task Stop();
}
