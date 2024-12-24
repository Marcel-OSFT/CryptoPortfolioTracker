using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
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
    private readonly PortfolioContext context;
    [ObservableProperty] private ObservableCollection<Coin> listCoins = new();
    private SortingOrder currentSortingOrder;
    private Func<Coin, object> currentSortFunc;

    public LibraryService(PortfolioContext portfolioContext)
    {
        context = portfolioContext;
        currentSortFunc = x => x.Rank;
        currentSortingOrder = SortingOrder.Ascending;
    }

    public async Task<ObservableCollection<Coin>> PopulateCoinsList()
    {
        var getResult = await GetCoinsFromContext();
        getResult.IfSucc(list =>
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListCoins = new ObservableCollection<Coin>(list.OrderBy(currentSortFunc));
            }
            else
            {
                ListCoins = new ObservableCollection<Coin>(list.OrderByDescending(currentSortFunc));
            }
        });
        getResult.IfFail(err => ListCoins = new());
        return ListCoins;
    }
    public async Task<ObservableCollection<Coin>> PopulateCoinsList( SortingOrder sortingOrder, Func<Coin, object> sortFunc )
    {
        var getResult = await GetCoinsFromContext();
        getResult.IfSucc(list =>
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListCoins = new ObservableCollection<Coin>(list.OrderBy(sortFunc));
            }
            else
            {
                ListCoins = new ObservableCollection<Coin>(list.OrderByDescending(sortFunc));
            }
            currentSortingOrder = sortingOrder;
            currentSortFunc = sortFunc;
        });
        getResult.IfFail(err => ListCoins = new());
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
        if (ListCoins is null) { return Task.FromResult(false); }
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
        if (coin == null) { return Task.FromResult(false); }
        try
        {
            ListCoins.Add(coin);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }

    public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    {
        if (ListCoins is not null)
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListCoins = new ObservableCollection<Coin>(ListCoins.OrderBy(sortFunc));
            }
            else
            {
                ListCoins = new ObservableCollection<Coin>(ListCoins.OrderByDescending(sortFunc));
            }
        }

        currentSortingOrder = sortingOrder;
        currentSortFunc = sortFunc;

    }







    /// <summary>
    /// this function without parameters will sort the list using the last used settings.
    /// </summary>
    public void SortList()
    {
        if (ListCoins is not null)
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListCoins = new ObservableCollection<Coin>(ListCoins.OrderBy(currentSortFunc));
            }
            else
            {
                ListCoins = new ObservableCollection<Coin>(ListCoins.OrderByDescending(currentSortFunc));
            }
        }
    }

    public async Task<Result<bool>> CreateCoin(Coin? newCoin)
    {
        var _result = false;

        if (newCoin == null) { return _result; }
        try
        {
            context.Coins.Add(newCoin);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        return _result;
    }

    public async Task<Result<bool>> MergeCoin(Coin prelistingCoin, Coin? newCoin)
    {
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
            return new Result<bool>(ex);
        }
        return _result;
    }

    public async Task<Result<Coin>> GetCoin(string coinId)
    {
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
        List<Coin>? coinList = null;
        try
        {
            //coinList = await context.Coins.OrderBy(x => x.Rank).ToListAsync();
            coinList = await context.Coins
                .Include(x => x.PriceLevels)
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
            return new Result<bool>(ex);
        }
        return _result;
    }

    public async Task<Result<List<CoinList>>> GetCoinListFromGecko()
    {
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

        Exception? error = null;
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
            return new Result<List<CoinList>>(error);
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
                    AddCoinDialog.Current?.ShowBePatienceNotice();
                }
                return default;
            }
        }).Build();

        CoinFullDataById? coinFullDataById = null;

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
        var serializerSettings = new JsonSerializerSettings();

        var coinsClient = new CoinGeckoClient(httpClient, App.CoinGeckoApiKey, App.ApiPath, serializerSettings);

        Exception? error = null;
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
            return new Result<CoinFullDataById>(error);
        }

        return coinFullDataById ?? new CoinFullDataById();
    }

    public async Task<Result<bool>> UpdateNote(Coin coin, string note)
    {
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

    public void RejectChanges()
    {
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
}
