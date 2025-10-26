namespace CryptoPortfolioTracker.Services;

public interface IIndicatorService
{
    Task CalculateRsiAsync(Coin coin);
    Task<double> CalculateMaAsync(Coin coin);
    void EvaluatePriceLevels(Coin coin, double newValue);
}