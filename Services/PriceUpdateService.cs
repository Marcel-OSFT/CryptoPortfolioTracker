using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
using HttpClient = System.Net.Http.HttpClient;

namespace CryptoPortfolioTracker.Services;

public class PriceUpdateService : IPriceUpdateService
{
    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cts = new();
    
    private readonly PortfolioService _portfolioService;
    private readonly IPreferencesService _preferencesService;
    private readonly IPriceLevelService _priceLevelService;
    private readonly IAssetService _assetService;
    private readonly IMessenger _messenger;

    private UpdateContext currentContext;
    private static ILogger Logger { get; set; } = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PriceUpdateService).Name.PadRight(22));
    public bool IsPausRequested { get; private set; }
    public bool IsUpdating { get; private set; }
    //private bool isInit;

    private Task? timerTask;
    private Task? resumeTask;



    public PriceUpdateService(PortfolioService portfolioService, IAssetService assetService, IPriceLevelService priceLevelService, IMessenger messenger, IPreferencesService preferencesService)
    {
        _portfolioService = portfolioService;
        currentContext = _portfolioService.UpdateContext;

        _preferencesService = preferencesService;
        _priceLevelService = priceLevelService;
        _assetService = assetService;
        _messenger = messenger;

        IsPausRequested = false;
        //timer = new(System.TimeSpan.FromMinutes(_preferencesService.GetRefreshIntervalMinutes()));
        timer = new(System.TimeSpan.FromMinutes(2));

    }

    public void Start()
    {
        Logger.Information("PriceUpdateService started");
        currentContext = _portfolioService.UpdateContext;
        IsUpdating = false;
        timerTask = DoWorkAsync();
    }

    private async Task DoWorkAsync()
    {
        try
        {
           // isInit = true;
            await UpdatePricesAllCoins();
           // isInit = false;
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (!IsPausRequested)
                {
                    if (resumeTask == null || resumeTask.IsCompleted)
                    {
                        resumeTask = null;
                        Logger.Information("NextTick received");
                        await UpdatePricesAllCoins();
                    }
                    else
                    {
                        Logger.Information("NextTick received => service waiting for resume");
                    }
                }
                else
                {
                    Logger.Information("NextTick received => service paused");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Information("PriceUpdateService cancelled");
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex, "PriceUpdateService stopped unexpected");
        }
    }

    public void Stop()
    {
        if (timerTask is null)
        {
            return;
        }
        cts.Cancel();
        cts.Dispose();
        Logger.Information("PriceUpdateService stopped");
    }

    public void Pause(bool isDisconnecting = false)
    {
        IsPausRequested = true;
        Logger.Information("PriceUpdateService Paused");
    }

    public void Resume()
    {
        IsPausRequested = false;
        if (currentContext != _portfolioService.UpdateContext)
        {
            resumeTask = Task.Run(async () =>
            {
                Logger.Information("PriceUpdateService continued with new context");
                currentContext = _portfolioService.UpdateContext;
                await UpdatePricesAllCoins();
            });
        }
        else
        {
            Logger.Information("PriceUpdateService continued with existing context");
        }
    }

    private async Task UpdatePricesAllCoins()
    {
        IsUpdating = true;

        await App.UpdateSemaphore.WaitAsync();
        bool isReleased = false;
        
        try
        {
            var context = _portfolioService.UpdateContext;
            var coinIdsTemp = await context.Coins
                //.AsNoTracking()
                .Where(x => x.Name.Length <= 12 || (x.Name.Length > 12 && x.Name.Substring(x.Name.Length - 12) != "_pre-listing"))
                .Include(x => x.PriceLevels)
                .Select(c => c.ApiId)
                .ToListAsync();

            isReleased = App.UpdateSemaphore.Release() == 0;

            if (!coinIdsTemp.Any())
            {
                return;
            }
            var coinIds = ShiftCoinIdsRandom(coinIdsTemp);

            var dataPerPage = 100;
            var nrOfPages = (int)Math.Ceiling((double)coinIds.Count / dataPerPage);
            var result = new Result<bool>();

            var coinIdsPerPage = SplitCoinIdsPerPageAndJoin(coinIds, dataPerPage, nrOfPages);

            for (var pageNr = 1; pageNr <= nrOfPages; pageNr++)
            {
                if (cts.IsCancellationRequested || IsPausRequested)
                {
                    return;
                }
                var coinMarketsResult = await GetMarketDataFromGecko(coinIdsPerPage[pageNr - 1], dataPerPage);
                // Await the result of IfSucc to ensure it completes before proceeding
                await coinMarketsResult.Match(
                    async list =>
                    {
                        result = await UpdatePricesWithMarketData(list);
                    },
                    err =>
                    {
                        result = new Result<bool>(err);
                        return Task.CompletedTask;
                    }
                    );

                if (nrOfPages > 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }

                MainPage.Current.DispatcherQueue.TryEnqueue( async () =>
                {
                    _assetService.SortList();
                    await _assetService.CalculateAssetsTotalValues();
                });
            }
            Logger.Information($"All coins are updated.");
            _priceLevelService.UpdateHeatMap();
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, $"UpdatePricesAllCoins failed.");
        }
        finally
        {
            if (!isReleased) 
            {
                App.UpdateSemaphore.Release();
            };
            IsUpdating = false;
        }
    }

    private static List<string> ShiftCoinIdsRandom(List<string> coinIds)
    {
        var startIndex = new Random().Next(0, coinIds.Count - 1);
        var shiftedCoinIds = new List<string>();

        for (var i = startIndex; i < coinIds.Count + startIndex; i++)
        {
            var j = i >= coinIds.Count ? i - coinIds.Count : i;
            shiftedCoinIds.Add(coinIds[j]);
        }
        return shiftedCoinIds;
    }

    private static string[] SplitCoinIdsPerPageAndJoin(List<string> coinIds, int dataPerPage, int nrOfPages)
    {
        var coinIdsPerPage = new string[nrOfPages];
        var dataToGo = coinIds.Count;

        for (var pageNr = 1; pageNr <= nrOfPages; pageNr++)
        {
            var dataToTake = dataToGo <= dataPerPage ? dataToGo : dataPerPage;
            coinIdsPerPage[pageNr - 1] = string.Join(",", coinIds.Skip((pageNr - 1) * dataPerPage).Take(dataToTake));
            dataToGo -= dataToTake;
        }
        return coinIdsPerPage;
    }

    private async Task<Result<List<CoinMarkets>>> GetMarketDataFromGecko(string coinIds, int dataPerPage)
    {
        var retries = 0;
        var totalRequests = 0;

        var strategy = new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<System.Exception>(),
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(30),
            OnRetry = args =>
            {
                Logger.Debug(args.Outcome.Exception!, "Getting Market Data; OnRetry ({0})", retries);
                retries++;
                return default;
            }
        }).Build();

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();
        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        List<CoinMarkets>? coinMarketsPage = null;

        var tokenSource2 = new CancellationTokenSource();
        var cancellationToken = tokenSource2.Token;
        while (!cancellationToken.IsCancellationRequested && !IsPausRequested)
        {
            totalRequests++;
            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    Logger.Debug("Getting Market Data; (Retries: {0})", retries);
                    coinMarketsPage = await coinsClient.Coins.Markets
                        .Ids(coinIds)
                        .VsCurrency("usd")
                        .Order("market_cap_desc")
                        .Page(1).PerPage(dataPerPage)
                        .IncludeSparkline(false)
                        .PriceChangePercentage("24h,30d,1y")
                        .GetAsync<List<CoinMarkets>>(token);
                }, cancellationToken);

                Logger.Information("Received Market Data; (Count: {0})", coinMarketsPage?.Count.ToString() ?? "0");
                
            }
            catch (System.Exception ex)
            {
                Logger.Warning(ex, "Getting Market Data failed after {0} requests", totalRequests);
                return new Result<List<CoinMarkets>>(ex);
            }
            finally
            {
                tokenSource2.Cancel();
                tokenSource2.Dispose();
            }
        }
        return coinMarketsPage ?? new Result<List<CoinMarkets>>(new NullReferenceException());
    }

    private async Task<Result<bool>> UpdatePricesWithMarketData(List<CoinMarkets> marketDataList)
    {
        if (marketDataList == null) return new Result<bool>(new ArgumentNullException(nameof(marketDataList)));

        Logger.Information($"Updating Market Data. {marketDataList.Count}");
        foreach (var coinData in marketDataList)
        {
            var coinResult = await UpdatePriceCoin(coinData);
            if (coinResult.IsFaulted)
            {
                return new Result<bool>(false);
            }
        }
        return true;
    }

    private async Task<Result<Coin>> UpdatePriceCoin(CoinMarkets coinData)
    {
        await App.UpdateSemaphore.WaitAsync();
        await Task.Delay(100);
        var context = _portfolioService.UpdateContext;
        context.ChangeTracker?.Clear();

        try
        {
            var coin = await context.Coins
                .Include(x => x.PriceLevels)
                .SingleAsync(c => c.ApiId.ToLower() == coinData.Id.ToLower());

            var oldPrice = coin.Price;
            var newPrice = coinData.CurrentPrice ?? 0;

            if (oldPrice != newPrice)
            {
                coin.Price = newPrice;
                coin.MarketCap = coinData.MarketCap ?? 0;
                coin.ImageUri = coinData.Image.AbsoluteUri?.Replace("large", "small") ?? string.Empty;
                coin.Rank = coinData.MarketCapRank ?? 999999;
                coin.Change24Hr = coinData.PriceChangePercentage24HInCurrency ?? 0;
                coin.Ath = coinData.Ath ?? 0;
                coin.Change1Month = coinData.PriceChangePercentage30DInCurrency ?? 0;
                coin.Change52Week = coinData.PriceChangePercentage1YInCurrency ?? 0;

                context.Coins.Update(coin);
                Logger.Information("Updating {0} {1} => {2}", coin.Name, oldPrice, newPrice);

                await context.SaveChangesAsync();

                //// Reflect the changes in the Coin entity also in the PortfolioContext
                var entity = await _portfolioService.Context.Coins.FindAsync(coin.Id);
                if (entity != null)
                {
                    await _portfolioService.Context.Entry(entity).ReloadAsync();
                }

                MainPage.Current.DispatcherQueue.TryEnqueue(() =>
                {
                    _messenger.Send(new UpdatePricesMessage(coin));
                });
            }
            return coin;
        }
        catch (JsonException jsonEx)
        {
            Logger.Error(jsonEx, "JSON processing error for coin: {0}", coinData.Name);
            return new Result<Coin>(jsonEx);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Updating Prices {coinData.Name} failed.");
            return new Result<Coin>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
            App.UpdateSemaphore.Release();
        }
    }


}


