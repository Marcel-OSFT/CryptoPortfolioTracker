using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using LanguageExt.ClassInstances;
using LanguageExt.Common;
using LanguageExt.Pipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Polly;
using Serilog;
using Serilog.Core;
using Windows.ApplicationModel.Appointments;
using Windows.UI;
using Windows.UI.Notifications;
using static System.Net.WebRequestMethods;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
using HttpClient = System.Net.Http.HttpClient;

namespace CryptoPortfolioTracker.Services;

public class PriceUpdateService : IPriceUpdateService, IDisposable
{
    private readonly PeriodicTimer timer;
    private readonly CancellationTokenSource cts = new();
    private Task? timerTask;
    private readonly PortfolioContext coinContext;
    private readonly IPreferencesService _preferencesService;
    private readonly IAssetService _assetService;

    private static ILogger Logger
    {
        get; set;
    }
    public bool IsPaused { get; set; }

    public PriceUpdateService(PortfolioContext portfolioContext, IAssetService assetService, IPreferencesService preferencesService)
    {
        Logger = Log.Logger.ForContext(Constants.SourceContextPropertyName, typeof(PriceUpdateService).Name.PadRight(22));
        coinContext = portfolioContext;
        _preferencesService = preferencesService;
        _assetService = assetService;
        timer = new (System.TimeSpan.FromMinutes(_preferencesService.GetRefreshIntervalMinutes()));
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
                if (!IsPaused)
                {
                    Logger.Information("NextTick received");
                    await UpdatePricesAllCoins();
                }
                else
                {
                    Logger.Information("NextTick received => service paused");
                }
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
    public async Task Stop()
    {
        if (timerTask is null)
        {
            return;
        }
        cts.Cancel();
        cts.Dispose();
        
        Logger.Information("PriceUpdateService stopped");
    }
    public void Pause()
    {
        IsPaused = true;
        Logger.Information("PriceUpdateService Paused");
    }
    public void Continue()
    {
        IsPaused = false;
        Logger.Information("PriceUpdateService Continued");
    }

    private async Task UpdatePricesAllCoins()
    {

        var coinIdsTemp = await coinContext.Coins.Where(x => x.Name.Length<=12 || (x.Name.Length>12 && x.Name.Substring(x.Name.Length-12) != "_pre-listing") ).Select(c => c.ApiId).ToListAsync();
        if (!coinIdsTemp.Any())
        {
            return;
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
            if (cts is null || cts.IsCancellationRequested || IsPaused)
            {
                return; //break in case GraphUpdateService has been canceled or paused
            }
            var coinMarketsResult = await GetMarketDataFromGecko(coinIdsPerPage[pageNr - 1], dataPerPage);
            coinMarketsResult.IfSucc(async list => result = await UpdatePricesWithMarketData(list));
            coinMarketsResult.IfFail(err => result = new Result<bool>(err));

            if (nrOfPages > 1)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }

            await coinContext.SaveChangesAsync();
            _assetService.SortList();
            await _assetService.CalculateAssetsTotalValues();
        }
        return;
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
            MaxRetryAttempts = 5,
            Delay = System.TimeSpan.FromSeconds(30), // Wait between each try
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                Logger.Debug(exception, "Getting Market Data; OnRetry ({0})", Retries.ToString());
                Retries++;
                return default;
            }
        }).Build();


        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();

        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        System.Exception? error = null;
        List<CoinMarkets>? coinMarketsPage = null;

        while (!cancellationToken.IsCancellationRequested && !IsPaused)
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

        if (marketDataList is null) { return new Result<bool>(error); }

        Logger.Information("Updating Market Data.");
        foreach (var coinData in marketDataList)
        {
            var coinResult = await UpdatePriceCoin(coinData);
            coinResult.IfFail(err => error = err);
        }
        //if (AssetsViewModel.Current is not null)
        //{
        //    //await AssetsViewModel.Current.CalculateAssetsTotalValues();
        //    await _assetService.CalculateAssetsTotalValues();
        //}
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
            Logger.Information("Updating {0} {1} => {2}", coin.Name, oldPrice, newPrice);

            if (AssetsViewModel.Current is not null)
            {
                Logger.Information("Updating Assets Overview");
                //AssetsViewModel.Current.UpdatePricesAssetTotals(coin, oldPrice, newPrice);
                _assetService.UpdatePricesAssetTotals(coin, oldPrice, newPrice);
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
        //GC.SuppressFinalize(this);
    }

}


