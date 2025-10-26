using CryptoPortfolioTracker.Infrastructure.Response.Coins;

namespace CryptoPortfolioTracker.Services;

public class IndicatorService : IIndicatorService
{
    private readonly Settings _settings;

    public IndicatorService(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task CalculateRsiAsync(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));

        try
        {
            var prices = await LoadClosingPricesAsync(coin);
            prices.Add(coin.Price);

            int period = _settings.RsiPeriod;
            if (period == 0 || prices == null || prices.Count < period + 1)
            {
                coin.Rsi = 0;
                return;
            }

            var rsiValues = new List<double>();
            var gains = new List<double>();
            var losses = new List<double>();

            for (int i = 1; i <= period; i++)
            {
                double change = prices[i] - prices[i - 1];
                if (change > 0)
                {
                    gains.Add(change);
                    losses.Add(0);
                }
                else
                {
                    gains.Add(0);
                    losses.Add(-change);
                }
            }

            double avgGain = gains.Average();
            double avgLoss = losses.Average();
            double alpha = 1.0 / period;

            double rs = avgGain / avgLoss;
            double rsi = 100 - (100 / (1 + rs));
            rsiValues.Add(rsi);

            for (int i = period + 1; i < prices.Count; i++)
            {
                double change = prices[i] - prices[i - 1];
                double gain = change > 0 ? change : 0;
                double loss = change < 0 ? -change : 0;

                avgGain = (gain * alpha) + (avgGain * (1 - alpha));
                avgLoss = (loss * alpha) + (avgLoss * (1 - alpha));

                rs = avgGain / avgLoss;
                rsi = 100 - (100 / (1 + rs));
                rsiValues.Add(rsi);
            }

            coin.Rsi = rsiValues.Last();
        }
        catch
        {
            coin.Rsi = 0;
        }
    }

    public async Task<double> CalculateMaAsync(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));

        var prices = await LoadClosingPricesAsync(coin);
        prices.Add(coin.Price);

        int period = _settings.MaPeriod;
        string maType = _settings.MaType;

        if (period == 0 || prices == null || prices.Count < period)
        {
            coin.Ema = 0;
            return 0;
        }

        if (string.Equals(maType, "SMA", StringComparison.OrdinalIgnoreCase))
        {
            double smaRecent = prices.TakeLast(period).Average();
            coin.Ema = smaRecent;
            UpdatePriceLevelEma(coin);
            return smaRecent;
        }

        // EMA calculation
        double sma = prices.Take(period).Average();
        double multiplier = 2.0 / (period + 1);
        double ema = sma;

        for (int i = period; i < prices.Count; i++)
        {
            ema = ((prices[i] - ema) * multiplier) + ema;
        }

        coin.Ema = ema;
        UpdatePriceLevelEma(coin);
        return ema;
    }

    public void EvaluatePriceLevels(Coin coin, double newValue)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));
        if (newValue == 0) return;

        var withinRangePerc = _settings.WithinRangePerc;
        var closeToPerc = _settings.CloseToPerc;

        foreach (var level in coin.PriceLevels)
        {
            if (level.Value == 0) continue;

            var dist = (100 * (newValue - level.Value) / level.Value);
            level.DistanceToValuePerc = dist;

            if ((level.Type == PriceLevelType.TakeProfit && newValue >= level.Value) ||
                (level.Type == PriceLevelType.Buy && newValue <= level.Value) ||
                (level.Type == PriceLevelType.Stop && newValue <= level.Value) ||
                (level.Type == PriceLevelType.Ema && newValue <= level.Value))
            {
                level.Status = PriceLevelStatus.TaggedPrice;
                continue;
            }

            if (dist >= -1 * closeToPerc)
            {
                level.Status = PriceLevelStatus.CloseToPrice;
                continue;
            }

            if (dist >= -1 * withinRangePerc)
            {
                level.Status = PriceLevelStatus.WithinRange;
                continue;
            }

            level.Status = PriceLevelStatus.NotWithinRange;
        }

        // Note: original model invoked OnPropertyChanged(nameof(PriceLevels)).
        // If UI binding requires a change notification for the collection itself,
        // caller should raise it (or Coin should expose a method to do so).
    }

    // --- Helpers ---

    private async Task<List<double>> LoadClosingPricesAsync(Coin coin)
    {
        // Loads market chart JSON and returns the recent closing prices (no caching here).
        try
        {
            var fileName = Path.Combine(AppConstants.ChartsFolder, $"MarketChart_{coin.ApiId}.json");
            if (!File.Exists(fileName))
                return new List<double>();

            var suffix = coin.Name.Contains("_pre-listing") ? "-prelisting" : "";
            var marketChart = new MarketChartById();
            await marketChart.LoadMarketChartJson(coin.ApiId + suffix);

            if (marketChart.Prices?.Length > 0)
                return marketChart.Prices.TakeLast(150).Select(p => (double)p[1].Value).ToList();

            return new List<double>();
        }
        catch
        {
            return new List<double>();
        }
    }

    private void UpdatePriceLevelEma(Coin coin)
    {
        var priceLevelEma = coin.PriceLevels.FirstOrDefault(p => p.Type == PriceLevelType.Ema);
        if (priceLevelEma != null)
        {
            priceLevelEma.Value = coin.Ema;
            if (coin.Ema != 0)
                priceLevelEma.DistanceToValuePerc = (100 * (coin.Price - coin.Ema) / coin.Ema);
        }
        else
        {
            var newPriceLevel = new PriceLevel
            {
                Type = PriceLevelType.Ema,
                Value = coin.Ema,
                Coin = coin,
                DistanceToValuePerc = coin.Ema != 0 ? (100 * (coin.Price - coin.Ema) / coin.Ema) : 0
            };
            coin.PriceLevels.Add(newPriceLevel);
        }
        // As above: if UI needs the PriceLevels property changed notification, raise it from the caller / model.
    }
}