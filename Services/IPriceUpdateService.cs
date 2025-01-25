using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IPriceUpdateService
{
    public bool IsUpdating { get; }
    public void Start();
    public void Pause(bool isDisconnecting = false);
    public void Resume();
    public void Stop();
}
