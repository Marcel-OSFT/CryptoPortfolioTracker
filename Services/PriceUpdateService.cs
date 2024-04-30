using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
using HttpClient = System.Net.Http.HttpClient;

namespace CryptoPortfolioTracker.Services;

public class PriceUpdateService : IPriceUpdateService, IDisposable
{
    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cts = new();
    private Task? timerTask;
    private readonly PortfolioContext coinContext;

    private static ILogger Logger
    {
        get; set;
    }

    public PriceUpdateService(PortfolioContext portfolioContext)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PriceUpdateService).Name.PadRight(22));
        coinContext = portfolioContext;
        timer = new (System.TimeSpan.FromMinutes(App.userPreferences.RefreshIntervalMinutes));
    }

    public void Start()
    {
        Logger.Information("PriceUpdateService started");
        timerTask = DoWorkAsync();
    }

    private async Task DoWorkAsync()
    {
        try
        {
            await UpdatePricesAllCoins(); // get them right away...           
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                Logger.Information("NextTick handled");
                await UpdatePricesAllCoins();
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Information("PriceUpdateService canceled");
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

    private async Task<Result<bool>> UpdatePricesAllCoins()
    {
        var coinIdsTemp = await coinContext.Coins.Select(c => c.ApiId).ToListAsync();
        if (!coinIdsTemp.Any())
        {
            return false;
        }

        var coinIds = ShiftCoinIdsRandom(coinIdsTemp);

        //cut nr of coins in pieces here instead of in GetMeket.... 
        // and update prices also in smaller portions.
        // this prevents having a huge request-url
        var dataPerPage = 100;
        var nrOfPages = Convert.ToInt16(Math.Ceiling((double)coinIds.Count / dataPerPage));
        var result = new Result<bool>() ;

        var coinIdsPerPage = SplitCoinIdsPerPageAndJoin(coinIds, dataPerPage, nrOfPages);

        for (var pageNr = 1; pageNr <= nrOfPages; pageNr++)
        {
            var coinMarketsResult = await GetMarketDataFromGecko(coinIdsPerPage[pageNr - 1], dataPerPage);
            coinMarketsResult.IfSucc(async list => result = await UpdatePricesWithMarketData(list));
            coinMarketsResult.IfFail(err => result = new Result<bool>(err));

            if (nrOfPages > 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            await coinContext.SaveChangesAsync();
        }
        return result;
    }

    private static List<string> ShiftCoinIdsRandom(List<string> _coinIds)
    {
        //*** shift the order of coinIds in the list to get a different URL each time
        //*** this will avoid using the cache
        var startIndex = new Random().Next(0, _coinIds.Count - 1);
        var shiftedCoinIds = new List<string>();

        int j;
        for (var i = startIndex; i < _coinIds.Count + startIndex; i++)
        {
            if (i >= _coinIds.Count)
            {
                j = i - _coinIds.Count;
            }
            else
            {
                j = i;
            }
            shiftedCoinIds.Add(_coinIds.ElementAt(j));
        }
        return shiftedCoinIds;
    }

    private static string[] SplitCoinIdsPerPageAndJoin(List<string> coinIds, int dataPerPage, int nrOfPages)
    {
        var coinIdsPerPage = new string[nrOfPages];
        var dataToGo = coinIds.Count;
        try
        {
            for (var pageNr = 1; pageNr <= nrOfPages; pageNr++)
            {
                var dataToTake = dataToGo <= dataPerPage ? dataToGo : dataPerPage;
                coinIdsPerPage[pageNr - 1] = string.Join(",", coinIds.ToArray(), (pageNr - 1) * dataPerPage, dataToTake);
                dataToGo -= dataToTake;
            }
        }
        catch (System.Exception ex)
        {
            Logger.Debug(ex, "SplitCoinIdsPerPageAndJoin");
        }
        return coinIdsPerPage;
    }

    private async Task<Result<List<CoinMarkets>>> GetMarketDataFromGecko(string coinIds, int dataPerPage)
    {
        var Retries = 0;
        var TotalRequests = 0;

        var tokenSource2 = new CancellationTokenSource();
        var cancellationToken = tokenSource2.Token;

        var strategy = new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<System.Exception>(),
            MaxRetryAttempts = 3,
            Delay = System.TimeSpan.FromSeconds(30), // Wait between each try
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                Logger.Debug(exception, "Getting Market Data; OnRetry ({0})", Retries.ToString());
                Retries++;
                return default;
            }
        }).Build();


        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();

        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        System.Exception? error = null;
        List<CoinMarkets>? coinMarketsPage = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            TotalRequests++;

            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    Logger.Debug("Getting Market Data; (Retries: {0})", Retries.ToString());

                    coinMarketsPage = await coinsClient.Coins.Markets
                        .Ids(coinIds)
                        .VsCurrency("usd")
                        .Order("market_cap_desc")
                        .Page(1).PerPage(dataPerPage)
                        .IncludeSparkline(false)
                        .PriceChangePercentage("24h,30d,1y")
                        .GetAsync<List<CoinMarkets>>(token);
                }, cancellationToken);

                var count = coinMarketsPage is not null ? coinMarketsPage.Count.ToString() : "0";
                Logger.Information("Received Market Data; (Count: {0})", count);
            }
            catch (System.Exception ex)
            {
                Logger.Warning(ex, "Getting Market Data failed after {0} requests - {1}", TotalRequests);
                error = ex;
            }
            finally
            {
                tokenSource2.Cancel();
                tokenSource2.Dispose();
            }
        }
        if (error != null)
        {
            return new Result<List<CoinMarkets>>(error);
        }

        return coinMarketsPage ?? new Result<List<CoinMarkets>>(new NullReferenceException());
    }

    private async Task<Result<bool>> UpdatePricesWithMarketData(List<CoinMarkets> marketDataList)
    {
        System.Exception? error = null;

        Logger.Information("Updating Market Data.");
        foreach (var coinData in marketDataList)
        {
            var coinResult = await UpdatePriceCoin(coinData);
            coinResult.IfFail(err => error = err);
        }
        AssetsViewModel.Current.CalculateAssetsTotalValues();
        return error == null ? true : new Result<bool>(error);
    }

    private async Task<Result<Coin>> UpdatePriceCoin(CoinMarkets coinData)
    {
        Coin coin;
        try
        {
            coin = await coinContext.Coins.Where(c => c.ApiId.ToLower() == coinData.Id.ToLower()).SingleAsync();
            var oldPrice = coin.Price;
            var newPrice = coinData.CurrentPrice;

            coin.Price = coinData.CurrentPrice ?? 0;
            coin.MarketCap = coinData.MarketCap ?? 0;
            coin.ImageUri = coinData.Image.AbsoluteUri ?? string.Empty;
            coin.Rank = coinData.MarketCapRank ?? 999999;
            coin.Change24Hr = coinData.PriceChangePercentage24HInCurrency ?? 0;
            coin.Ath = coinData.Ath ?? 0;
            coin.Change1Month = coinData.PriceChangePercentage30DInCurrency ?? 0;
            coin.Change52Week = coinData.PriceChangePercentage1YInCurrency ?? 0;

            coinContext.Coins.Update(coin);

            var list = AssetsViewModel.Current.ListAssetTotals;
            var asset = list?.Where(a => a.Coin.Id == coin.Id).SingleOrDefault();
            if (asset != null && list != null && oldPrice != newPrice)
            {
                Logger.Information("Updating {0} {1} => {2}", coin.Name, oldPrice, newPrice);
                var index = -1;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Coin.Id == asset.Coin.Id)
                    {
                        index = i;
                        break;
                    }
                }
                list[index].Coin = coin;
                list[index].MarketValue = list[index].Qty * coin.Price;
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex, "Updating Prices {0} failed", coinData.Name);
            return new Result<Coin>(ex);
        }
        return coin;
    }

    public void Dispose() // Implement IDisposable
    {
        Stop();
        timer.Dispose();
        GC.SuppressFinalize(this);
    }

}


