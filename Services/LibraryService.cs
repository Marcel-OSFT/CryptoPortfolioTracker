using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using CoinGeckoClient = CoinGeckoFluentApi.Client.CoinGeckoClient;
using Exception = System.Exception;

namespace CryptoPortfolioTracker.Services;

public partial class LibraryService : ObservableObject, ILibraryService
{
    private readonly IMessenger _messenger;
    private readonly PortfolioService _portfolioService;
    [ObservableProperty] private ObservableCollection<Coin> listCoins = new();
    private SortingOrder currentSortingOrder;
    private Func<Coin, object> currentSortFunc;

    public LibraryService(PortfolioService portfolioService, IMessenger messenger)
    {
        _portfolioService = portfolioService;
        _messenger = messenger;
        currentSortFunc = x => x.Rank;
        currentSortingOrder = SortingOrder.Ascending;
    }

    public async Task<ObservableCollection<Coin>> PopulateCoinsList( SortingOrder sortingOrder, Func<Coin, object> sortFunc )
    {
        var getResult = await GetCoinsFromContext();
        ListCoins = getResult.Match(
            list => SortedList(list, sortingOrder, sortFunc),
            err =>  new());
        
        return ListCoins;
    }

    public bool IsCoinsListEmpty()
    {
        return !ListCoins.Any();
    }

    public void ClearCoinsList()
    {
        ListCoins.Clear();
    }

    public Task RemoveFromCoinsList(Coin coin)
    {
        if (ListCoins is null || !ListCoins.Any()) { return Task.FromResult(false); }
        try
        {
            ListCoins.Remove(coin);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    public Task AddToCoinsList(Coin coin)
    {
        if (coin == null || !ListCoins.Any()) { return Task.FromResult(false); }
        try
        {
            ListCoins.Add(coin);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }

    public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    {
        if (ListCoins is null || !ListCoins.Any()) return;
        
        var list = ListCoins.ToList();
        ListCoins = SortedList(list,sortingOrder, sortFunc);

    }

    /// <summary>
    /// this function without parameters will sort the list using the last used settings.
    /// </summary>

    private ObservableCollection<Coin> SortedList(List<Coin> list, SortingOrder sortingOrder = SortingOrder.None, Func<Coin, object>? sortFunc = null)
    {
        if (sortingOrder != SortingOrder.None && sortFunc != null)
        {
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;
        }
        return currentSortingOrder == SortingOrder.Ascending
            ? new ObservableCollection<Coin>(list.OrderBy(currentSortFunc))
            : new ObservableCollection<Coin>(list.OrderByDescending(currentSortFunc));
    }



    public async Task<Result<bool>> CreateCoin(Coin? newCoin)
    {
        var context = _portfolioService.Context;
        var _result = false;

        if (newCoin == null) { return _result; }
        try
        {
            context.Coins.Add(newCoin);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        return _result;
    }

    public async Task<Result<bool>> MergeCoin(Coin prelistingCoin, Coin? newCoin)
    {
        var context = _portfolioService.Context;
        var _result = false;

        if (newCoin == null || prelistingCoin == null) { return _result; }
        try
        {
            var plCoin = await context.Coins.Where(x => x.ApiId.ToLower() == prelistingCoin.ApiId.ToLower()).SingleAsync();
            plCoin.ApiId = newCoin.ApiId;
            plCoin.Name = newCoin.Name;
            plCoin.Symbol = newCoin.Symbol;
            plCoin.About = newCoin.About;
            plCoin.ImageUri = newCoin.ImageUri;
            plCoin.Price = newCoin.Price;
            context.Coins.Update(plCoin);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        return _result;
    }

    public async Task<Result<Coin>> GetCoin(string coinId)
    {
        var context = _portfolioService.Context;
        Coin coin;
        if (coinId == null || coinId == "") { return new Coin(); }
        try
        {
            coin = await context.Coins
                .Include (x => x.PriceLevels)
                .Where(x => x.ApiId.ToLower() == coinId.ToLower())
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<Coin>(ex);
        }
        return coin;
    }

    public async Task<Result<List<Coin>>> GetCoinsFromContext()
    {
        var context = _portfolioService.Context;
        List<Coin>? coinList = null;
        try
        {
            //coinList = await context.Coins.OrderBy(x => x.Rank).ToListAsync();
            coinList = await context.Coins
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .OrderBy(x => x.Rank)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<Coin>>(ex);
        }
        return coinList is not null ? coinList : new List<Coin>();
    }

    public async Task<Result<bool>> RemoveCoin(Coin coin)
    {
        var context = _portfolioService.Context;
        bool _result;
        if (coin == null) { return false; }
        try
        {
            var coinToRemove = await context.Coins
                .Include(x => x.PriceLevels)
                .Where(x => x.ApiId == coin.ApiId)
                .SingleAsync();

            context.Coins.Remove(coinToRemove);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        return _result;
    }

    public async Task<Result<List<CoinList>>> GetCoinListFromGecko()
    {
        var context = _portfolioService.Context;
        var Retries = 0;

        var tokenSource2 = new CancellationTokenSource();
        var cancellationToken = tokenSource2.Token;

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

        List<CoinList>? coinList = null;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();

        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    coinList = await coinsClient.Coins.List.GetAsync<List<CoinList>>(token);

                }, cancellationToken);
            }
            catch (System.Exception ex)
            {
                return new Result<List<CoinList>>(ex);
            }
            finally
            {
                tokenSource2.Cancel();
                tokenSource2.Dispose();
            }
        }
        return coinList ?? new List<CoinList>();
    }

    public async Task<Result<CoinFullDataById>> GetCoinDetails(string coinId)
    {
        var Retries = 0;
        if (coinId == null || coinId == "") { return new Result<CoinFullDataById>(); }

        var tokenSource2 = new CancellationTokenSource();
        var cancellationToken = tokenSource2.Token;

        var strategy = new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            MaxRetryAttempts = 8,
            Delay = System.TimeSpan.FromSeconds(15), // Wait between each try
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                Retries++;
                if (Retries > 0)
                {
                    MainPage.Current.DispatcherQueue.TryEnqueue(() =>
                    {
                        _messenger.Send(new ShowBePatienceMessage());
                    });
                    
                }
                return default;
            }
        }).Build();

        CoinFullDataById? coinFullDataById = null;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();

        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await strategy.ExecuteAsync(async token =>
                {
                    coinFullDataById = await coinsClient.Coins[coinId].GetAsync<CoinFullDataById>(token);
                }, cancellationToken);
            }
            catch (System.Exception ex)
            {
                return new Result<CoinFullDataById>(ex);
            }
            finally
            {
                tokenSource2.Cancel();
                tokenSource2.Dispose();
            }
        }
        return coinFullDataById ?? new CoinFullDataById();
    }

    public async Task<Result<bool>> UpdateNote(Coin coin, string note)
    {
        var context = _portfolioService.Context;
        bool result;
        if (coin.Note == note) { return false; }
        try
        {
            coin.Note = note;
            context.Coins.Update(coin);
            result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        return result;
    }

    public bool IsNotAsset(Coin coin)
    {
        var context = _portfolioService.Context;
        List<Asset> assets; 

        if (coin == null || coin.ApiId == "") { return false; }
        try
        {
            assets = context.Assets.Where(x => x.Coin.ApiId.ToLower() == coin.ApiId.ToLower()).ToList();
        }
        catch (Exception)
        {
            return false;
        }
        return !assets.Any();
    }


    private void RejectChanges()
    {
        var context = _portfolioService.Context;
        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.State = EntityState.Modified; //Revert changes made to deleted entity.
                    entry.State = EntityState.Unchanged;
                    break;
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
            }
        }
    }

    public Portfolio? GetPortfolio()
    {
        return _portfolioService.CurrentPortfolio;
    }
}
