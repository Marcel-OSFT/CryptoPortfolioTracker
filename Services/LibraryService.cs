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
using CryptoPortfolioTracker.Helpers;
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

public interface ILibraryService
{
    ObservableCollection<Coin> ListCoins { get; set; }

    public Task<Result<bool>> CreateCoin(Coin? newCoin);
    public Task<Result<bool>> MergeCoin(Coin prelistingCoin, Coin? newCoin);
    public Task<Result<Coin>> GetCoin(string coinId);
    public Task<Result<List<Coin>>> GetCoinsFromContext();
    public Task<Result<CoinFullDataById>> GetCoinDetails(string coinId);
    public Task<Result<bool>> RemoveCoin(Coin coin);
    public Task<Result<List<CoinList>>> GetCoinListFromGecko();
    public Task<Result<bool>> UpdateNote(Coin coin, string note);
    public bool IsNotAsset(Coin coin);
    bool IsCoinsListEmpty();
    void ClearCoinsList();
    Task<bool> RemoveFromCoinsList(Coin coin);
    Task<bool> AddToCoinsList(Coin coin);
    Task<ObservableCollection<Coin>> PopulateCoinsList();
    //Portfolio? GetPortfolio();
    Task<ObservableCollection<Coin>> UpdateCoinsList(int coinId);
}

public partial class LibraryService : ObservableObject, ILibraryService
{
    private readonly IMessenger _messenger;
    public ObservableCollection<Coin> ListCoins { get; set; } = new();
    private readonly IAppState _appState;

    private PortfolioContext Context => _appState.Context;

    public LibraryService(IAppState appState, IMessenger messenger)
    {
        _messenger = messenger;
        _appState = appState;
        _appState.ContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Context));
        };
    }

    public async Task<ObservableCollection<Coin>> PopulateCoinsList()
    {
        var getResult = await GetCoinsFromContext();
        ListCoins = getResult.Match(
            list => new ObservableCollection<Coin>(list),
            err =>  new());
        
        return ListCoins;
    }

    public bool IsCoinsListEmpty()
    {
        return !ListCoins.Any();
    }

    public void ClearCoinsList()
    {
        ListCoins = null;
        OnPropertyChanged(nameof(ListCoins));
    }

    public Task<bool> RemoveFromCoinsList(Coin coin)
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

    public Task<bool> AddToCoinsList(Coin coin)
    {
        if (coin == null || !ListCoins.Any()) { return Task.FromResult(false); }
        try
        {
            ListCoins.Add(coin);
        }
        catch (Exception) { return Task.FromResult(false); }

        return Task.FromResult(true);
    }

    public async Task<ObservableCollection<Coin>> UpdateCoinsList(int coinId)
    {
        ObservableCollection<Coin> updated = new();

        if (ListCoins is null || !ListCoins.Any()) { return updated ; }
        try
        {
            var context = Context;

            var coinToUpdate = ListCoins.Where(x => x.Id == coinId).Single();
            var updatedCoin = context.Coins.AsNoTracking()
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .Where(x => x.Id == coinId)
                .SingleOrDefault();
            
            var index = ListCoins.IndexOf(coinToUpdate);
            ListCoins[index] = updatedCoin;
           
            updated.Add(updatedCoin);
        }
        catch (Exception) { return new ObservableCollection<Coin>(); }

        return updated;
    }

    //public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    //{
    //    if (ListCoins is null || !ListCoins.Any()) return;
        
    //    var list = ListCoins.ToList();
    //    ListCoins = SortedList(list,sortingOrder, sortFunc);

    //}

    /// <summary>
    /// this function without parameters will sort the list using the last used settings.
    /// </summary>

    //private ObservableCollection<Coin> SortedList(List<Coin> list, SortingOrder sortingOrder = SortingOrder.None, Func<Coin, object>? sortFunc = null)
    //{
    //    if (sortingOrder != SortingOrder.None && sortFunc != null)
    //    {
    //        currentSortFunc = sortFunc;
    //        currentSortingOrder = sortingOrder;
    //    }
    //    return currentSortingOrder == SortingOrder.Ascending
    //        ? new ObservableCollection<Coin>(list.OrderBy(currentSortFunc))
    //        : new ObservableCollection<Coin>(list.OrderByDescending(currentSortFunc));
    //}



    public async Task<Result<bool>> CreateCoin(Coin? newCoin)
    {
        var context = Context;
        context.ChangeTracker?.Clear();
        var _result = false;

        if (newCoin == null) { return _result; }
        try
        {
            //ensure that the Narrative is tracked before adding the Coin.
            // Using 'Find' will result in adding it to the tracked entities.
            var trackedNarrative = context.Narratives.Where(x => x.Id == newCoin.Narrative.Id).ToList().FirstOrDefault();
            newCoin.Narrative = trackedNarrative;
            context.Coins.Add(newCoin);

            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _result;
    }

    public async Task<Result<bool>> MergeCoin(Coin prelistingCoin, Coin? newCoin)
    {
        var context = Context;
        context.ChangeTracker?.Clear();
        var _result = false;

        if (newCoin == null || prelistingCoin == null) { return _result; }
        try
        {
            //*** to prevent tracking issues first get the narrative
            var narrative = await context.Narratives.SingleOrDefaultAsync(x => x.Id == newCoin.Narrative.Id);
            var plCoin = await context.Coins.Where(x => x.ApiId.ToLower() == prelistingCoin.ApiId.ToLower()).SingleAsync();
            plCoin.ApiId = newCoin.ApiId;
            plCoin.Name = newCoin.Name;
            plCoin.Symbol = newCoin.Symbol;
            plCoin.About = newCoin.About;
            plCoin.ImageUri = newCoin.ImageUri;
            plCoin.Price = newCoin.Price;
            plCoin.Narrative = narrative;
            
            context.Coins.Update(plCoin);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _result;
    }

    public async Task<Result<Coin>> GetCoin(string coinId)
    {
        var context = Context;
        Coin coin;
        if (coinId == null || coinId == "") { return new Coin(); }
        try
        {
            coin = await context.Coins
                .AsNoTracking()
                //.Include (x => x.PriceLevels)
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
        var context = Context;
        List<Coin>? coinList = null;
        try
        {
            //coinList = await context.Coins.OrderBy(x => x.Rank).ToListAsync();
            coinList = await context.Coins
                .AsNoTracking()
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
        var context = Context;
        context.ChangeTracker?.Clear();
        bool _result;
        if (coin == null) { return false; }
        try
        {
            var coinToRemove = await context.Coins
                .Where(x => x.ApiId == coin.ApiId)
                .Include(x => x.PriceLevels)
                .SingleAsync();

            context.Coins.Remove(coinToRemove);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
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

        var coinsClient = new CoinGeckoClient(httpClient, AppConstants.CoinGeckoApiKey, AppConstants.ApiPath, serializerSettings);

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

        var coinsClient = new CoinGeckoClient(httpClient, AppConstants.CoinGeckoApiKey, AppConstants.ApiPath, serializerSettings);

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
        var context = Context;
        context.ChangeTracker?.Clear();
        bool result;
        if (coin.Note == note) { return false; }
        try
        {
            var coinToUpdate = await context.Coins
                .Where(x => x.ApiId == coin.ApiId)
                .Include(x => x.PriceLevels)
                .SingleAsync();

            coinToUpdate.Note = note;
            context.Coins.Update(coinToUpdate);
            result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return result;
    }

    public bool IsNotAsset(Coin coin)
    {
        var context = Context;
        List<Asset> assets; 

        if (coin == null || coin.ApiId == "") { return false; }
        try
        {
            assets = context.Assets
                .AsNoTracking()
                .Where(x => x.Coin.ApiId.ToLower() == coin.ApiId.ToLower())
                .ToList();
        }
        catch (Exception)
        {
            return false;
        }
        return !assets.Any();
    }


    private void RejectChanges()
    {
        var context = Context;
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
