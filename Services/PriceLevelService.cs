
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
using LanguageExt;
using WinRT.CryptoPortfolioTrackerGenericHelpers;

namespace CryptoPortfolioTracker.Services;

public partial class PriceLevelService : ObservableObject, IPriceLevelService
{
    private readonly IMessenger _messenger;

    private HeatMap heatMap;
    private readonly IIndicatorService _indicatorService;
    private readonly PortfolioService _portfolioService;
    private readonly IAssetService _assetService;
    [ObservableProperty] public partial ObservableCollection<Coin> ListCoins { get; set; } = new();
    private SortingOrder currentSortingOrder;
    private Func<Coin, object> currentSortFunc;

    public PriceLevelService(PortfolioService portfolioService, IAssetService assetService, IMessenger messenger, IIndicatorService indicatorService)
    {
        _indicatorService = indicatorService;
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

    //public async Task<ObservableCollection<Coin>> PopulateCoinsList()
    //{
    //    var getResult = await GetCoinsFromContext();
    //    ListCoins = getResult.Match(
    //        list => new ObservableCollection<Coin>(SortedList(list)),
    //        err => ListCoins = new());

    //    return ListCoins;
    //}

    public async Task PopulateCoinsList()
    {
        var getResult = await GetCoinsFromContext();
        ListCoins = getResult.Match(
            list =>
            {
                if (!list.Any()) return new();
                if (currentSortFunc(list.First()) is double)
                {
                    //*** extract the Infinity values from the collection
                    //*** so that they appended at the end of the sorted collection
                    var infinityValues = list.Where(x => double.IsInfinity((double)currentSortFunc(x))).ToList();
                    var valuesList = list.Where(x => !double.IsInfinity((double)currentSortFunc(x))).ToList();

                    return new ObservableCollection<Coin>(SortedList(valuesList, currentSortingOrder, currentSortFunc).Concat(infinityValues));
                }
                else
                {
                    return new ObservableCollection<Coin>(SortedList(list, currentSortingOrder, currentSortFunc));
                }
            },
            err => new());
    }



    //public async Task<ObservableCollection<Coin>> PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object> sortFunc)
    public async Task PopulateCoinsList(SortingOrder sortingOrder, Func<Coin, object>? sortFunc)
    {
        var getResult = await GetCoinsFromContext();
        ListCoins = getResult.Match(
            list =>
            {
                //*** extract the Infinity values from the collection
                //*** so that they appended at the end of the sorted collection
                var infinityValues = list.Where(x => double.IsInfinity((double)sortFunc(x))).ToList();
                var valuesList = list.Where(x => !double.IsInfinity((double)sortFunc(x))).ToList();

                return new ObservableCollection<Coin>(SortedList(valuesList, sortingOrder, sortFunc).Concat(infinityValues));
            },
            err => new());
    }

    public async Task UpdateCoinsList(Coin coin, Coin updatedCoin)
    {
        if (ListCoins is null || !ListCoins.Any() || coin is null || updatedCoin is null) { return; }
        try
        {
            //var context = _portfolioService.Context;

            updatedCoin.Ema = await _indicatorService.CalculateMaAsync(updatedCoin);
            _indicatorService.EvaluatePriceLevels(updatedCoin, updatedCoin.Price);

            var coinToUpdateIndex = ListCoins.IndexOf(coin);
            //var updatedCoin = context.Coins.AsNoTracking()
            //    .Where(x => x.Id == coin.Id)
            //    .SingleOrDefault();

            ListCoins.RemoveAt(coinToUpdateIndex);
            ListCoins.Insert(coinToUpdateIndex, updatedCoin);
        }
        catch (Exception ex) 
        { 
            Task.FromResult(false); 
        }

        return;
    }

    public bool ListCoinsHasAny()
    {
        return ListCoins != null && ListCoins.Any();
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
                coin.Ema = await _indicatorService.CalculateMaAsync(coin);
                _indicatorService.EvaluatePriceLevels(coin, coin.Price);
            }
        }
        catch (Exception ex)
        {
            return new Result<List<Coin>>(ex);
        }
        return coinList is not null ? coinList : new List<Coin>();
    }
    public async Task<Result<Coin>> GetCoinFromContext(Coin coin)
    {
        var context = _portfolioService.Context;
        Coin? _coin;
        try
        {
            //coinList = await context.Coins.OrderBy(x => x.Rank).ToListAsync();
            _coin = await context.Coins
                .AsNoTracking()
                .Where(x => x.ApiId == coin.ApiId)
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .Include(x => x.Assets)
                .ThenInclude(x => x.Account)
                .OrderBy(x => x.Rank)
                .FirstAsync();

            _coin.Ema = await _indicatorService.CalculateMaAsync(_coin);
            _indicatorService.EvaluatePriceLevels(_coin, _coin.Price);
            
        }
        catch (Exception ex)
        {
            return new Result<Coin>(ex);
        }
        return _coin is not null ? _coin : new Coin();
    }

    public async Task<Either<Error,Coin>> ResetPriceLevels(Coin coin)
    {
        if (coin == null) return Error.New("Coin is null"); ;

       // await App.UpdateSemaphore.WaitAsync();
        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        bool result;

        Coin coinToReset;
        try
        {
            //ensure that the Coin is tracked before updating the Coin.
            // Using 'Find' will result in adding it to the tracked entities.
            coinToReset = context.Coins.Where(x => x.ApiId == coin.ApiId).Include(x => x.PriceLevels).First();

            foreach (var level in coinToReset.PriceLevels)
            {
                level.Value = 0;
                level.Note = string.Empty;
                level.Status = PriceLevelStatus.NotWithinRange;
                level.DistanceToValuePerc = 0;
            }

            context.Coins.Update(coinToReset);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return Error.New(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
            //App.UpdateSemaphore.Release();
        }

        return coinToReset;
    }

    public async Task<Either<Error,Coin>> UpdatePriceLevels(Coin coin, ICollection<PriceLevel> priceLevels)
    {
        //await App.UpdateSemaphore.WaitAsync();
        Debug.WriteLine("PriceLevels Locked");

        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        //bool result;

        Coin coinToUpdate;
        if (coin == null || priceLevels == null || priceLevels.Count == 0)
        {
            //App.UpdateSemaphore.Release();
            return Error.New("Coin is null");
        }
        try
        {
            coinToUpdate = await context.Coins
                .Where(x => x.ApiId == coin.ApiId)
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .Include(x => x.Assets)
                .ThenInclude(x => x.Account)
                .FirstAsync(); 


            //ensure that the Coin is tracked before updating the Coin.
            // Using 'Find' will result in adding it to the tracked entities.
            //foreach (var level in priceLevels)
            //{
            //    var priceLevelToUpdate = coin.PriceLevels.First(x => x.Type == level.Type);
            //    priceLevelToUpdate.Value = level.Value;
            //    priceLevelToUpdate.Note = level.Note;
            //}
            coinToUpdate.PriceLevels = priceLevels;

            _indicatorService.EvaluatePriceLevels(coinToUpdate, coinToUpdate.Price);

            context.Coins.Update(coinToUpdate);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            RejectChanges();
            return Error.New(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
            //App.UpdateSemaphore.Release();
        }
        return coinToUpdate;
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
                else if (selectedHeatMapIndex == 1)
                {
                    
                    index = AddHeatMapPointRsi(index, heatMapPoints, sumMarketValue, asset);
                }
                else if (selectedHeatMapIndex == 2)
                {
                    // Add other heat map points if needed
                    index = AddHeatMapPointEma(index, heatMapPoints, sumMarketValue, asset);
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

    private static int AddHeatMapPointEma(int index, ObservableCollection<HeatMapPoint> heatMapPoints, double sumMarketValue, AssetTotals? asset)
    {
        if (asset.Coin.PriceLevels is null || asset.Coin.PriceLevels.Count == 0)
        {
            return index;
        }

        var priceLevel = asset.Coin.PriceLevels.Where(x => x.Type == PriceLevelType.Ema).FirstOrDefault();

        if (priceLevel == null)
        {
            return index;
        }

        priceLevel.DistanceToValuePerc = (100 * (asset.Coin.Price - priceLevel.Value) / priceLevel.Value);

        var perc = priceLevel.DistanceToValuePerc;

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
        //ListCoins?.Clear();
        ListCoins = null;
        OnPropertyChanged(nameof(ListCoins));
    }

}
