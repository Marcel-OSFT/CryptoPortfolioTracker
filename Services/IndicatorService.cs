using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services;

public interface IIndicatorService
{
    Task<double> CalculateRsiAsync(Coin coin);
    Task<double> CalculateMaAsync(Coin coin);
    double GetRsi(Coin coin);
    double GetEma(Coin coin);
}

public class IndicatorService : IIndicatorService
{
    private readonly Settings _settings;

    // Cache lifetime is service-scoped, not EF-scoped
    private readonly ConcurrentDictionary<string, List<double>> _closingPricesCache = new();
    private readonly ConcurrentDictionary<string, DateTime> _closingPricesFileDateCache = new();
    private readonly ConcurrentDictionary<string, double> _rsiCache = new();
    private readonly ConcurrentDictionary<string, double> _emaCache = new();

    public IndicatorService(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public double GetRsi(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));
        return _rsiCache.TryGetValue(coin.ApiId, out var value) ? value : 0;
    }

    public double GetEma(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));
        return _emaCache.TryGetValue(coin.ApiId, out var value) ? value : 0;
    }

    public async Task<double> CalculateRsiAsync(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));

        try
        {
            var prices = await GetClosingPricesAsync(coin);
            prices = AppendCurrentPriceForCalculationOnly(prices, coin.Price);

            int period = _settings.RsiPeriod;
            if (period == 0 || prices == null || prices.Count < period + 1)
            {
                _rsiCache[coin.ApiId] = 0; 
                return 0;
            }

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

            double rs = avgLoss == 0 ? 0 : avgGain / avgLoss;
            double rsi = avgLoss == 0 ? 100 : 100 - (100 / (1 + rs));

            for (int i = period + 1; i < prices.Count; i++)
            {
                double change = prices[i] - prices[i - 1];
                double gain = change > 0 ? change : 0;
                double loss = change < 0 ? -change : 0;

                avgGain = (gain * alpha) + (avgGain * (1 - alpha));
                avgLoss = (loss * alpha) + (avgLoss * (1 - alpha));

                rs = avgLoss == 0 ? 0 : avgGain / avgLoss;
                rsi = avgLoss == 0 ? 100 : 100 - (100 / (1 + rs));
            }

            return _rsiCache[coin.ApiId] = rsi;
        }
        catch
        {
            return _rsiCache[coin.ApiId] = 0;
        }
    }

    public async Task<double> CalculateMaAsync(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));

        try
        {
            var prices = await GetClosingPricesAsync(coin);
            prices = AppendCurrentPriceForCalculationOnly(prices, coin.Price);

            int period = _settings.MaPeriod;
            string maType = _settings.MaType;

            if (period == 0 || prices == null || prices.Count < period)
            {
                return _emaCache[coin.ApiId] = 0;
            }

            if (string.Equals(maType, "SMA", StringComparison.OrdinalIgnoreCase))
            {
                double smaRecent = prices.TakeLast(period).Average();
                return _emaCache[coin.ApiId] = smaRecent;
            }

            double sma = prices.Take(period).Average();
            double multiplier = 2.0 / (period + 1);
            double ema = sma;

            for (int i = period; i < prices.Count; i++)
            {
                ema = ((prices[i] - ema) * multiplier) + ema;
            }

            return _emaCache[coin.ApiId] = ema;
        }
        catch
        {
            return _emaCache[coin.ApiId] = 0;
        }
    }

    

    private async Task<List<double>> GetClosingPricesAsync(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));

        var apiId = coin.ApiId;
        var fileName = Path.Combine(AppConstants.ChartsFolder, $"MarketChart_{apiId}.json");

        if (!File.Exists(fileName))
        {
            _closingPricesCache[apiId] = new List<double>();
            _closingPricesFileDateCache[apiId] = DateTime.MinValue;
            return new List<double>();
        }

        var lastWriteUtc = File.GetLastWriteTimeUtc(fileName);

        if (_closingPricesCache.TryGetValue(apiId, out var cachedPrices) &&
            _closingPricesFileDateCache.TryGetValue(apiId, out var cachedFileDate) &&
            cachedFileDate == lastWriteUtc)
        {
            return cachedPrices.ToList(); // return copy, not shared reference
        }

        var suffix = coin.Name.Contains("_pre-listing") ? "-prelisting" : "";
        var marketChart = new MarketChartById();

        await marketChart.LoadMarketChartJson(apiId + suffix);

        var prices = marketChart.Prices != null && marketChart.Prices.Length > 0
            ? marketChart.Prices.TakeLast(150).Select(p => (double)p[1].Value).ToList()
            : new List<double>();

        _closingPricesCache[apiId] = prices;
        _closingPricesFileDateCache[apiId] = lastWriteUtc;

        return prices.ToList(); // copy again to avoid accidental mutation
    }

    private static List<double> AppendCurrentPriceForCalculationOnly(List<double> prices, double currentPrice)
    {
        if (prices == null)
        {
            prices = new List<double>();
        }

        var working = prices.ToList();

        if (working.Count == 0 || Math.Abs(working.Last() - currentPrice) > 0.0000001)
        {
            working.Add(currentPrice);
        }

        return working;
    }

    
}