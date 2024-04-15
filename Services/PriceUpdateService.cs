//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
using HttpClient = System.Net.Http.HttpClient;

namespace CryptoPortfolioTracker.Services;

public class PriceUpdateService : IPriceUpdateService, IDisposable
{
    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cts = new();
    private Task timerTask;
    readonly IServiceScope currentContextScope;
    readonly PortfolioContext coinContext;

    ILogger Logger  { get; set; }

    public PriceUpdateService(TimeSpan timerInterval)
    {
        //Logger = Log.Logger.ForContext<PriceUpdateService>();
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PriceUpdateService).Name.PadRight(22));

        currentContextScope = null;
        coinContext = App.Container.GetService<PortfolioContext>();
        timer = new(timerInterval);
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
        catch (OperationCanceledException ex) 
        {
            Logger.Information("PriceUpdateService canceled");
        }
        catch (Exception ex)
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
        var coinIds = await coinContext.Coins.Select(c => c.ApiId).ToListAsync();
        if (!coinIds.Any()) return false;

        //cut nr of coins in pieces here instead of in GetMeket.... 
        // and update prices also in smaller portions.
        // this prevents having a huge request-url
        int dataPerPage = 100;
        int nrOfPages = Convert.ToInt16(Math.Ceiling((double)coinIds.Count / dataPerPage));
        Result<bool> result = new Result<bool>();

        string[] coinIdsPerPage = SplitCoinIdsPerPageAndJoin(coinIds, dataPerPage, nrOfPages);

        for (int pageNr = 1; pageNr <= nrOfPages; pageNr++)
        {
            var coinMarketsResult = await GetMarketDataFromGecko(coinIdsPerPage[pageNr - 1], dataPerPage);
            result = coinMarketsResult.Match<Result<bool>>(
               Succ: m => UpdatePricesWithMarketData(m).Result,
               Fail: err => new Result<bool>(err));

            if (nrOfPages > 1) await Task.Delay(TimeSpan.FromSeconds(30));
            await coinContext.SaveChangesAsync();
        }
        return result;
    }
    private string[] SplitCoinIdsPerPageAndJoin(List<string> coinIds, int dataPerPage, int nrOfPages)
    {
        string[] coinIdsPerPage = new string[nrOfPages];
        int dataToGo = coinIds.Count;
        try
        {
            for (int pageNr = 1; pageNr <= nrOfPages; pageNr++)
            {
                int dataToTake = dataToGo <= dataPerPage ? dataToGo : dataPerPage;
                coinIdsPerPage[pageNr - 1] = string.Join(",", coinIds.ToArray(), (pageNr - 1) * dataPerPage, dataToTake);
                dataToGo -= dataToTake;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
        return coinIdsPerPage;
    }

    private async Task<Result<List<CoinMarkets>>> GetMarketDataFromGecko(string coinIds, int dataPerPage)
    {
        //int EventualSuccesses = 0;
        int Retries = 0;
        //int EventualFailures = 0;
        int TotalRequests = 0;

        CancellationTokenSource tokenSource2 = new CancellationTokenSource();
        CancellationToken cancellationToken = tokenSource2.Token;

        var strategy = new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
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


        HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

        CoinGeckoClient coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);
        //bool isValidResult;

        Exception error = null;
        List<CoinMarkets> coinMarketsPage = null;

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
                        .GetAsync<List<CoinMarkets>>();
                }, cancellationToken);

                Logger.Information("Received Market Data; (Count: {0})", coinMarketsPage.Count.ToString());
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
        if (error != null) return new Result<List<CoinMarkets>>(error);

        return coinMarketsPage;
    }

    private async Task<Result<bool>> UpdatePricesWithMarketData(List<CoinMarkets> marketDataList)
    {
        Exception error = null;

        Logger.Information("Updating Market Data.");
        foreach (var coinData in marketDataList)
        {
            var coinResult = await UpdatePriceCoin(coinData);
            coinResult.IfFail(err => error = err);
        }
        AssetsViewModel.Current.CalculateAssetsTotalValues();
        return error != null ? true : new Result<bool>(error);
    }

    private async Task<Result<Coin>> UpdatePriceCoin(CoinMarkets coinData)
    {
        Coin coin = null;
        try
        {
            coin = await coinContext.Coins.Where(c => c.ApiId.ToLower() == coinData.Id.ToLower()).SingleAsync();
            var oldPrice = coin.Price;
            var newPrice = coinData.CurrentPrice;

            coin.Price = coinData.CurrentPrice != null ? (double)coinData.CurrentPrice : 0;
            coin.MarketCap = coinData.MarketCap != null ? (double)coinData.MarketCap : 0;
            coin.ImageUri = coinData.Image.AbsoluteUri != null ? coinData.Image.AbsoluteUri : "";
            coin.Rank = coinData.MarketCapRank != null ? (long)coinData.MarketCapRank : 999999;
            coin.Change24Hr = coinData.PriceChangePercentage24HInCurrency != null ? (double)coinData.PriceChangePercentage24HInCurrency : 0;
            coin.Ath = coinData.Ath != null ? (double)coinData.Ath : 0;
            coin.Change1Month = coinData.PriceChangePercentage30DInCurrency != null ? (double)coinData.PriceChangePercentage30DInCurrency : 0;
            coin.Change52Week = coinData.PriceChangePercentage1YInCurrency != null ? (double)coinData.PriceChangePercentage1YInCurrency : 0;

            coinContext.Coins.Update(coin);

            var list = AssetsViewModel.Current.ListAssetTotals;
            var asset = list.Where(a => a.Coin.Id == coin.Id).SingleOrDefault();
            if (asset != null && oldPrice != newPrice)
            {
                Logger.Information("Updating {0} {1} => {2}", coin.Name, oldPrice, newPrice);
                int index = -1;
                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i].Coin.Id == asset.Coin.Id)
                    {
                        index = i;
                        break;
                    }
                }
                AssetsViewModel.Current.ListAssetTotals[index].Coin = coin;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Updating Prices {0} failed", coinData.Name);
            return new Result<Coin>(ex);
        }
        return coin;
    }
    

    public void Dispose()
    {
        Stop();
        timer.Dispose();
        
    }


}


