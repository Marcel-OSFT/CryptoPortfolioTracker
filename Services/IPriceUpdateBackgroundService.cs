namespace CryptoPortfolioTracker.Services
{
    public interface IPriceUpdateBackgroundService
    {
        public void Start();
        public void Stop();

    }
}
