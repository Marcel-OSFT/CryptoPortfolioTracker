using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IPriceUpdateService
{
    public bool IsUpdating { get; }
    public void Start();
    public void Pause();
    public Task Resume();
    public Task Stop();
}
