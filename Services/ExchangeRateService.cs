using CoinGeckoFluentApi.Client;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using Newtonsoft.Json;
using Polly;

namespace CryptoPortfolioTracker.Services
{
    public interface IExchangeRateService
    {
        double EurUsdRate { get; }
        DateTimeOffset LastUpdated { get; }
        string LastUpdatedDisplay { get; }

        event PropertyChangedEventHandler? PropertyChanged;

        Task CopyExchangeRatesMarketChartIfNeeded();
        Task LoadDailyExchangeRates(DateTime fromDate);
        Task<Result<double>> RefreshAsync();
        bool TryGetDailyRate(DateOnly date, out double rate);
    }

    public sealed class ExchangeRateService : IExchangeRateService, INotifyPropertyChanged
    {
        private readonly List<DailyExchangeRate> _dailyExchangeRates = new();
        private readonly Dictionary<int, double> _dailyExchangeRateLookup = new();
        private readonly IMessenger _messenger;
        private DateTime _lastMissingRateWarningUtc = DateTime.MinValue;

        public IReadOnlyList<DailyExchangeRate> DailyExchangeRates => _dailyExchangeRates;

        public ExchangeRateService(IMessenger messenger)
        {
            _messenger = messenger;
            //LoadDailyExchangeRates(DateTime.MinValue).GetAwaiter().GetResult();
        }

        private double _eurUsdRate;
        public double EurUsdRate
        {
            get => _eurUsdRate;
            private set
            {
                if (_eurUsdRate != value)
                {
                    _eurUsdRate = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EurUsdRate)));
                }
            }
        }

        private DateTimeOffset _lastUpdated;
        private bool hasLoadRatesCalled = false;

        public DateTimeOffset LastUpdated
        {
            get => _lastUpdated;
            private set
            {
                if (_lastUpdated != value)
                {
                    _lastUpdated = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastUpdated)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastUpdatedDisplay)));
                }
            }
        }

        public string LastUpdatedDisplay => LastUpdated == default ? "Not updated yet" : LastUpdated.ToLocalTime().ToString("dd-MM-yyyy HH:mm:ss");

       

        public async Task<Result<double>> RefreshAsync()
        {
            var tokenSource2 = new CancellationTokenSource();
            var cancellationToken = tokenSource2.Token;

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");

            var Retries = 0;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 8,
                Delay = System.TimeSpan.FromSeconds(15), // Wait between each try
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    Retries++;
                    return default;
                }
            }).Build();

            CoinGeckoExchangeRatesResponse exchangeRates = new();

            var serializerSettings = new JsonSerializerSettings();

            var coinsClient = new CoinGeckoClient(httpClient, AppConstants.CoinGeckoApiKey, AppConstants.ApiPath, serializerSettings);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await strategy.ExecuteAsync(async token =>
                    {
                        exchangeRates = await coinsClient.ExchangeRates.GetAsync<CoinGeckoExchangeRatesResponse>(token);

                    }, cancellationToken);
                   
                }
                catch (System.Exception ex)
                {
                    return new Result<double>(ex);
                }
                finally
                {
                    tokenSource2.Cancel();
                    tokenSource2.Dispose();
                }
            }

            var usdRate = exchangeRates.Rates["usd"].Value;
            var eurRate = exchangeRates.Rates["eur"].Value;

            EurUsdRate = usdRate / eurRate;  // ~1.1204
            LastUpdated = DateTimeOffset.UtcNow;

            return new Result<double>(EurUsdRate);
        }

        public async Task CopyExchangeRatesMarketChartIfNeeded()
        {
            var sourceFilePath = Path.Combine(AppConstants.AppPath + "\\Assets\\MarketChart_ExchangeRates.json");
            var destinationFilePath = Path.Combine(AppConstants.ChartsFolder, "MarketChart_ExchangeRates.json");
            if (File.Exists(sourceFilePath) && !File.Exists(destinationFilePath))
            {
                File.Copy(sourceFilePath, destinationFilePath);
            }
        }

        public async Task LoadDailyExchangeRates(DateTime fromDate)
        {
            hasLoadRatesCalled = true;

            var sourceFilePath = Path.Combine(AppConstants.ChartsFolder, "MarketChart_ExchangeRates.json");
            if (!File.Exists(sourceFilePath))
            {
                return;
            }

            var json = File.ReadAllText(sourceFilePath);
            var rawDailyRates = JsonConvert.DeserializeObject<List<double[]>>(json);
            if (rawDailyRates == null)
            {
                return;
            }

            _dailyExchangeRates.Clear();
            _dailyExchangeRateLookup.Clear();

            var fromDateOnly = DateOnly.FromDateTime(fromDate.Date);

            foreach (var dataPoint in rawDailyRates)
            {
                if (dataPoint == null || dataPoint.Length < 2)
                {
                    continue;
                }

                var unixMilliseconds = (long)dataPoint[0];
                var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime);
                if (date < fromDateOnly)
                {
                    continue;
                }

                var rate = dataPoint[1];
                var dailyRate = new DailyExchangeRate(date, rate);

                _dailyExchangeRates.Add(dailyRate);
                _dailyExchangeRateLookup[date.DayNumber] = rate;
            }
        }

        public bool TryGetDailyRate(DateOnly date, out double rate)
        {
            if (!hasLoadRatesCalled)
            {
                rate = 0;
                return false;
            }

            if (_dailyExchangeRateLookup.TryGetValue(date.DayNumber, out rate))
            {
                return true;
            }

            if (_dailyExchangeRates.Count == 0)
            {
                var now = DateTime.UtcNow;
                if (now - _lastMissingRateWarningUtc > TimeSpan.FromMinutes(1))
                {
                    _messenger.Send(new ExchangeRateMissingMessage(date, "No historical exchange rate data has been loaded."));
                    _lastMissingRateWarningUtc = now;
                }

                rate = 0;
                return false;
            }

            var closestRate = _dailyExchangeRates
                .OrderBy(x => Math.Abs((long)x.Date.DayNumber - date.DayNumber))
                .First();

            rate = closestRate.Rate;

            var warningTime = DateTime.UtcNow;
            if (warningTime - _lastMissingRateWarningUtc > TimeSpan.FromMinutes(1))
            {
                _messenger.Send(new ExchangeRateMissingMessage(
                    date,
                    $"No exact daily exchange rate available for {date:dd-MM-yyyy}; using the closest available rate from {closestRate.Date:dd-MM-yyyy}."));
                _lastMissingRateWarningUtc = warningTime;
            }

            return true;
        }

       

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
