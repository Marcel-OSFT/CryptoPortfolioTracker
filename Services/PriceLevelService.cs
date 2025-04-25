
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Enums;
using System.Collections.ObjectModel;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt.Common;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using LiveChartsCore.Defaults;
using System.Diagnostics;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using CommunityToolkit.Mvvm.Messaging;
using System.Data.SqlClient;
using CryptoPortfolioTracker.Dialogs;
using WinRT;
using CryptoPortfolioTracker.Helpers;

namespace CryptoPortfolioTracker.Services;

public partial class PriceLevelService : ObservableObject, IPriceLevelService
{
    private readonly IMessenger _messenger;

    private HeatMap heatMap;
    private readonly PortfolioService _portfolioService;
    private readonly IAssetService _assetService;
    [ObservableProperty] public partial ObservableCollection<Coin> ListCoins { get; set; } = new();
    private SortingOrder currentSortingOrder;
    private Func<Coin, object> currentSortFunc;

    public PriceLevelService(PortfolioService portfolioService, IAssetService assetService, IMessenger messenger)
    {
        _portfolioService = portfolioService;
        _assetService= assetService;
        _messenger = messenger;
        currentSortFunc = x => x.Rank;
        currentSortingOrder = SortingOrder.Ascending;

        heatMap = new HeatMap();
    }

    public void UpdateHeatMap()
    {
        MainPage.Current.DispatcherQueue.TryEnqueue(() =>
        {
            _messenger.Send(new UpdateDashboardMessage());
        });
        
    }

    public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    {
        if (ListCoins is null || !ListCoins.Any()) return;
        
        //*** extract the Infinity values from the collection
        //*** so that they appended at the end of the sorted collection
        var infinityValues = ListCoins.Where(x => double.IsInfinity((double)sortFunc(x))).ToList();
        var valueList = ListCoins.Where(x => !double.IsInfinity((double)sortFunc(x))).ToList();

        ListCoins = new ObservableCollection<Coin>(SortedList(valueList, sortingOrder, sortFunc).Concat(infinityValues)); ;
    }
    public void SortListString(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    {
        if (ListCoins is null || !ListCoins.Any()) return;
        var list = ListCoins.ToList();
        ListCoins = new ObservableCollection<Coin>(SortedList(list, sortingOrder, sortFunc));
    }


    public bool IsCoinsListEmpty()
    {
        return !ListCoins.Any();
    }

    public async Task<ObservableCollection<Coin>> PopulateCoinsList()
    {
        var getResult = await GetCoinsFromContext();
        ListCoins = getResult.Match(
            list => new ObservableCollection<Coin>(SortedList(list)),
            err => ListCoins = new());
        
        return ListCoins;
    }
    public async Task<ObservableCollection<Coin>> PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    {
        var getResult = await GetCoinsFromContext();
        ListCoins = getResult.Match(list =>
        {
            //*** extract the Infinity values from the collection
            //*** so that they appended at the end of the sorted collection
            var infinityValues = list.Where(x => double.IsInfinity((double)sortFunc(x))).ToList();
            var valuesList = list.Where(x => !double.IsInfinity((double)sortFunc(x))).ToList();

            return new ObservableCollection<Coin>(SortedList(valuesList, sortingOrder, sortFunc).Concat(infinityValues));
        },
        err => new());

        return ListCoins;
    }

    public Task UpdateCoinsList(Coin coin)
    {
        if (ListCoins == null) return Task.CompletedTask;

        var coinToUpdate = ListCoins.Where(a => a.ApiId == coin.ApiId).SingleOrDefault();
        if (coinToUpdate != null)
        {
            var index = -1;
            for (var i = 0; i < ListCoins.Count; i++)
            {
                if (ListCoins[i].ApiId == coinToUpdate.ApiId)
                {
                    index = i;
                    break;
                }
            }
            ListCoins[index] = coin;
        }
        return Task.CompletedTask;
    }




    private List<Coin> SortedList(List<Coin> list, SortingOrder sortingOrder = SortingOrder.None, Func<Coin, object>? sortFunc = null)
    {
        if (sortingOrder != SortingOrder.None && sortFunc != null)
        {
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;
        }
        return currentSortingOrder == SortingOrder.Ascending
            ? new List<Coin>(list.OrderBy(currentSortFunc))
            : new List<Coin>(list.OrderByDescending(currentSortFunc));
    }

    public async Task<Result<List<Coin>>> GetCoinsFromContext()
    {
        var context = _portfolioService.Context;
        List<Coin>? coinList = null;
        try
        {
            //coinList = await context.Coins.OrderBy(x => x.Rank).ToListAsync();
            coinList = await context.Coins
                .AsNoTracking()
                .Where(x => x.Name.Length <= 12 || (x.Name.Length > 12 && x.Name.Substring(x.Name.Length - 12) != "_pre-listing"))
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .Include(x => x.Assets)
                .ThenInclude(x => x.Account)
                .OrderBy(x => x.Rank)
                .ToListAsync();

            foreach (var coin in coinList)
            {
                coin.EvaluatePriceLevels(coin.Price);
            }
        }
        catch (Exception ex)
        {
            return new Result<List<Coin>>(ex);
        }
        return coinList is not null ? coinList : new List<Coin>();
    }

    public async Task<Result<bool>> ResetPriceLevels(Coin coin)
    {
        if (coin == null) return false;

        await App.UpdateSemaphore.WaitAsync();
        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        bool result;

        try
        {
            //ensure that the Coin is tracked before updating the Coin.
            // Using 'Find' will result in adding it to the tracked entities.
            context.Coins.Find(coin.Id);

            foreach (var level in coin.PriceLevels)
            {
                level.Value = 0;
                level.Note = string.Empty;
                level.Status = PriceLevelStatus.NotWithinRange;
                level.DistanceToValuePerc = 0;
            }

            context.Coins.Update(coin);
            result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
            App.UpdateSemaphore.Release();
        }

        return result;
    }

    public async Task<Result<bool>> UpdatePriceLevels(Coin coin, ICollection<PriceLevel> priceLevels)
    {
        await App.UpdateSemaphore.WaitAsync();
        Debug.WriteLine("PriceLevels Locked");

        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        bool result;
        if (coin == null || priceLevels == null || priceLevels.Count == 0)
        {
            App.UpdateSemaphore.Release();
            return false;
        }
        try
        {
            //ensure that the Coin is tracked before updating the Coin.
            // Using 'Find' will result in adding it to the tracked entities.
            context.Coins.Find(coin.Id);
            foreach (var level in priceLevels)
            {
                var priceLevelToUpdate = coin.PriceLevels.First(x => x.Type == level.Type);
                priceLevelToUpdate.Value = level.Value;
                priceLevelToUpdate.Note = level.Note;
            }

            context.Coins.Update(coin);
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
            App.UpdateSemaphore.Release();
        }
        return result;
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

    public async Task<ObservableCollection<HeatMapPoint>> GetHeatMapPoints(int selectedHeatMapIndex)
    {
        var heatMapPoints = new ObservableCollection<HeatMapPoint>();

        try
        {
            var assets = (await _assetService.PopulateAssetTotalsList()).Where(x => x.MarketValue > 0).OrderBy(x => x.Coin.Rank).ToList();
            var sumMarketValue = assets.Sum(x => x.MarketValue);
            int index = 0;

            foreach (var asset in assets)
            {
                if (selectedHeatMapIndex == 0)
                {
                    index = AddHeatMapPointTarget(index, heatMapPoints, sumMarketValue, asset);
                }
                else
                {
                    
                    index = AddHeatMapPointRsi(index, heatMapPoints, sumMarketValue, asset);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }

        return heatMapPoints;
    }

    private static int AddHeatMapPointRsi(int index, ObservableCollection<HeatMapPoint> heatMapPoints, double sumMarketValue, AssetTotals? asset)
    {
        var rsi = asset.Coin.Rsi;

        var weight = 100 * asset.MarketValue / sumMarketValue;

        if (!double.IsInfinity(rsi))
        {
            var hmPoint = new HeatMapPoint
            {
                X = index += 1,
                Y = rsi,
                Weight = weight,
                Label = asset.Coin.Symbol
            };
            heatMapPoints.Add(hmPoint);
        }
        return index;
    }

    private static int AddHeatMapPointTarget(int index, ObservableCollection<HeatMapPoint> heatMapPoints, double sumMarketValue, AssetTotals? asset)
    {
        if (asset.Coin.PriceLevels is null || asset.Coin.PriceLevels.Count == 0) 
        { 
            return index; 
        }

        
        var priceLevel = asset.Coin.PriceLevels.Where(x => x.Type == PriceLevelType.TakeProfit).First();

        priceLevel.DistanceToValuePerc = (100 * (asset.Coin.Price - priceLevel.Value) / priceLevel.Value);

        var perc = priceLevel.DistanceToValuePerc;
        //var perc = asset.Coin.PriceLevels.Where(x => x.Type == PriceLevelType.TakeProfit).First().DistanceToValuePerc;


        var weight = 100 * asset.MarketValue / sumMarketValue;

        if (!double.IsInfinity(perc))
        {
            var hmPoint = new HeatMapPoint
            {
                X = index += 1,
                Y = perc,
                // Y = -1 * perc,
                Weight = weight,
                Label = asset.Coin.Symbol
            };
            heatMapPoints.Add(hmPoint);
        }
        return index;
    }

    public Portfolio GetPortfolio()
    {
        return _assetService.GetPortfolio();
    }

    public void ClearCoinsList()
    {
        ListCoins?.Clear();
    }

}
