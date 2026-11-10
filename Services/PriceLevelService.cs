using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Dialogs;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.ViewModels;
using CryptoPortfolioTracker.Views;
using LanguageExt;
using LanguageExt.Common;
using LiveChartsCore.Defaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WinRT;
using WinRT.CryptoPortfolioTrackerGenericHelpers;

namespace CryptoPortfolioTracker.Services;

public interface IPriceLevelService
{
    ObservableCollection<Coin> ListCoins { get; set; }
    public void SortList(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    public bool IsCoinsListEmpty();
    public Task<ObservableCollection<Coin>> PopulateCoinsList();
    public Task<Result<List<Coin>>> GetCoinsFromContext();
    public Task<Either<Error, Coin>> ResetPriceLevels(Coin coin);
    public Task<Either<Error, Coin>> UpdatePriceLevels(Coin coin, ICollection<PriceLevel> priceLevels);
    Task<ObservableCollection<HeatMapPoint>> GetHeatMapPoints(BubbleMap bubbleMap);
    void SortListString(SortingOrder sortingOrder, Func<Coin, object> sortFunc);
    Portfolio GetPortfolio();
    Task<ObservableCollection<Coin>> UpdateCoinsList(Coin coin);
    void ClearCoinsList();
    bool ListCoinsHasAny();
    Task<ObservableCollection<Coin>> PopulateCoinsListAsync();
    Task EvaluatePriceLevelsAsync(Coin coin);
}

public partial class PriceLevelService : ObservableObject, IPriceLevelService
{
    private readonly IMessenger _messenger;
    private readonly Settings _settings;
    private readonly IAppState _appState;

    private PortfolioContext Context => _appState.Context;

    private Portfolio CurrentPortfolio => _appState.CurrentPortfolio;

    private HeatMap heatMap;
    private readonly IIndicatorService _indicatorService;
    private readonly IAssetService _assetService;
    [ObservableProperty] public partial ObservableCollection<Coin> ListCoins { get; set; } = new();
    private SortingOrder currentSortingOrder;
    private Func<Coin, object> currentSortFunc;

    public PriceLevelService(IAppState appState, IAssetService assetService, IMessenger messenger, IIndicatorService indicatorService, Settings settings)
    {
        _indicatorService = indicatorService;
        _assetService= assetService;
        _messenger = messenger;
        _settings = settings;
        currentSortFunc = x => x.Rank;
        currentSortingOrder = SortingOrder.Ascending;

        heatMap = new HeatMap();
        _appState = appState;
        _appState.ContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Context));
        };

        _appState.CurrentPortfolioChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(CurrentPortfolio));
        };

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

    public async Task<ObservableCollection<Coin>> PopulateCoinsListAsync()
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
        return ListCoins;
    }

    public async Task<ObservableCollection<Coin>> PopulateCoinsList()
    {
        try
        {
            var getResult = await GetCoinsFromContext();
            ListCoins = getResult.Match(
                list =>
                {
                    //*** extract the Infinity values from the collection
                    //*** so that they appended at the end of the sorted collection
                    var infinityValues = new List<Coin>(); //list.Where(x => double.IsInfinity((double)sortFunc(x))).ToList();
                    var valuesList = list; //.Where(x => !double.IsInfinity((double)sortFunc(x))).ToList();

                    return new ObservableCollection<Coin>(valuesList.Concat(infinityValues));
                },
                err => new());

        }
        catch (Exception ex)
        {
            //*** Log the exception and return an empty collection
        }
        return ListCoins;
    }

    public async Task<ObservableCollection<Coin>> UpdateCoinsList(Coin coin)
    {
        var updated = new ObservableCollection<Coin>();

        if (ListCoins is null || !ListCoins.Any() || coin is null) { return new ObservableCollection<Coin>(); }
        try
        {
            await UpdatePriceLevelEma(coin);
            await EvaluatePriceLevelsAsync(coin);

            var context = Context;

            var coinToUpdate = ListCoins.Where(x => x.Id == coin.Id).Single();
            var updatedCoin = context.Coins.AsNoTracking()
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .Where(x => x.Id == coin.Id)
                .SingleOrDefault();

            var index = ListCoins.IndexOf(coinToUpdate);
            ListCoins[index] = updatedCoin;
            updated.Add(updatedCoin);
        }
        catch (Exception ex) 
        { 
            return new ObservableCollection<Coin>();
        }

        return updated;
    }

    public bool ListCoinsHasAny()
    {
        return ListCoins != null && ListCoins.Any();
    }



    private List<Coin> SortedList(List<Coin> list, SortingOrder sortingOrder = SortingOrder.None, Func<Coin, object>? sortFunc = null)
    {
        var sortedList = new List<Coin>();
        try
        {
            if (sortingOrder != SortingOrder.None && sortFunc != null)
            {
                currentSortFunc = sortFunc;
                currentSortingOrder = sortingOrder;
            }
            sortedList = currentSortingOrder == SortingOrder.Ascending
                ? new List<Coin>(list.OrderBy(currentSortFunc))
                : new List<Coin>(list.OrderByDescending(currentSortFunc));
        }
        catch (Exception ex)
        {
            //*** Log the exception and return the original list
            return list;
        } 


        return sortedList;
    }

    public async Task<Result<List<Coin>>> GetCoinsFromContext()
    {
        var context = Context;
        var stablecoinNarrative = await context.Narratives.Where(x => x.Name.ToLower() == "stablecoin").FirstOrDefaultAsync();
        List<Coin>? coinList = null;
        try
        {
            coinList = await context.Coins
                .AsNoTracking()
                .Where(x => (x.Name.Length <= 12 || (x.Name.Length > 12 && x.Name.Substring(x.Name.Length - 12) != "_pre-listing")) && x.Narrative != stablecoinNarrative)
                .Include(x => x.PriceLevels)
                .Include(x => x.Narrative)
                .Include(x => x.Assets)
                .ThenInclude(x => x.Account)
                .OrderBy(x => x.Rank)
                .ToListAsync();

            foreach (var coin in coinList)
            {
                //coin.Ema = await _indicatorService.CalculateMaAsync(coin);
                await UpdatePriceLevelEma(coin);
                await EvaluatePriceLevelsAsync(coin);
            }
        }
        catch (Exception ex)
        {
            return new Result<List<Coin>>(ex);
        }
        return coinList is not null ? coinList : new List<Coin>();
    }

    private async Task UpdatePriceLevelEma(Coin coin)
    {
        var priceLevelEma = coin.PriceLevels.FirstOrDefault(p => p.Type == PriceLevelType.Ema);
        var emaValue = _indicatorService.GetEma(coin);
        if (priceLevelEma != null)
        {
            priceLevelEma.Value = emaValue;
            if (priceLevelEma.Value != 0)
                priceLevelEma.DistanceToValuePerc = (100 * (coin.Price - priceLevelEma.Value) / priceLevelEma.Value);
        }
        else
        {
            var newPriceLevel = new PriceLevel
            {
                Type = PriceLevelType.Ema,
                Value = emaValue,
                Coin = coin,
                DistanceToValuePerc = emaValue != 0 ? (100 * (coin.Price - emaValue) / emaValue) : 0
            };
            coin.PriceLevels.Add(newPriceLevel);
        }
    }

    public async Task EvaluatePriceLevelsAsync(Coin coin)
    {
        if (coin is null) throw new ArgumentNullException(nameof(coin));
        var newValue = coin.Price;
        if (newValue == 0) return;

        var withinRangePerc = _settings.WithinRangePerc;
        var closeToPerc = _settings.CloseToPerc;

        foreach (var level in coin.PriceLevels)
        {
            if (level.Value == 0) continue;

            var dist = (100 * (newValue - level.Value) / level.Value);
            level.DistanceToValuePerc = dist;

            if ((level.Type == PriceLevelType.TakeProfit && newValue >= level.Value) ||
                (level.Type == PriceLevelType.Buy && newValue <= level.Value) ||
                (level.Type == PriceLevelType.Stop && newValue <= level.Value) ||
                (level.Type == PriceLevelType.Ema && newValue <= level.Value))
            {
                level.Status = PriceLevelStatus.TaggedPrice;
                continue;
            }

            if (dist >= -1 * closeToPerc)
            {
                level.Status = PriceLevelStatus.CloseToPrice;
                continue;
            }

            if (dist >= -1 * withinRangePerc)
            {
                level.Status = PriceLevelStatus.WithinRange;
                continue;
            }

            level.Status = PriceLevelStatus.NotWithinRange;
        }
    }

    public async Task<Either<Error,Coin>> ResetPriceLevels(Coin coin)
    {
        if (coin == null) return Error.New("Coin is null"); ;

        var context = Context;
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
        }

        return coinToReset;
    }

    public async Task<Either<Error,Coin>> UpdatePriceLevels(Coin coin, ICollection<PriceLevel> priceLevels)
    {
        Debug.WriteLine("PriceLevels Locked");

        var context = Context;
        context.ChangeTracker?.Clear();

        Coin coinToUpdate;
        if (coin == null || priceLevels == null || priceLevels.Count == 0)
        {
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

            await EvaluatePriceLevelsAsync(coinToUpdate);

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

    private async Task EvaluatePriceLevels()
    {
        var coins = Context.Coins
            .AsNoTracking()
            .ToList();

        foreach (var coin in coins)
        {
            await EvaluatePriceLevelsAsync(coin);
        }
    }

    public async Task<ObservableCollection<HeatMapPoint>> GetHeatMapPoints(BubbleMap bubbleMap)
    {
        var heatMapPoints = new ObservableCollection<HeatMapPoint>();

        try
        {
            var context = Context;
            var scNarrative = context.Narratives.Where(x => x.Name == "Stablecoin").FirstOrDefault();
            //*** Get all assets with MarketValue > 0 and not Stablecoins, ordered by Rank
            //*** exclude also the pre-listing coins from the heatmap, as they are not tradable yet
            var assets = (await _assetService.PopulateAssetTotalsList()).Where(x => x.MarketValue > 0 && x.Coin.Narrative != scNarrative && !x.Coin.Name.EndsWith("pre-listing")).OrderBy(x => x.Coin.Rank).ToList();

            var sumMarketValue = assets.Sum(x => x.MarketValue);
            int index = 0;

            foreach (var asset in assets)
            {
                if (bubbleMap.Type == BubbleMapType.Target)
                {
                    await EvaluatePriceLevels();
                    index = AddHeatMapPointTarget(index, heatMapPoints, sumMarketValue, asset);
                }
                else if (bubbleMap.Type == BubbleMapType.Rsi)
                {

                    index = AddHeatMapPointRsi(index, heatMapPoints, sumMarketValue, asset);
                }
                else if (bubbleMap.Type == BubbleMapType.Ma)
                {
                    // Add other heat map points if needed
                    await EvaluatePriceLevels();
                    index = AddHeatMapPointEma(index, heatMapPoints, sumMarketValue, asset);
                }
                else if (bubbleMap.Type == BubbleMapType.BreakEven)
                {
                    // Add other heat map points if needed
                    index = AddHeatMapPointBreakEven(index, heatMapPoints, sumMarketValue, asset);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }

        return heatMapPoints;
    }

    private int AddHeatMapPointRsi(int index, ObservableCollection<HeatMapPoint> heatMapPoints, double sumMarketValue, AssetTotals? asset)
    {

        if (asset is null)
        {
            return index;
        }
        var rsi = _indicatorService.GetRsi(asset.Coin);

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
        if (asset?.Coin?.PriceLevels is null || asset.Coin.PriceLevels.Count == 0)
        {
            return index;
        }

        var priceLevel = asset.Coin.PriceLevels.Where(x => x.Type == PriceLevelType.TakeProfit).First();

        priceLevel.DistanceToValuePerc = (100 * (asset.Coin.Price - priceLevel.Value) / priceLevel.Value);

        var perc = priceLevel.DistanceToValuePerc;

        var weight = 100 * asset.MarketValue / sumMarketValue;

        if (!double.IsInfinity(perc))
        {
            var hmPoint = new HeatMapPoint
            {
                X = index += 1,
                Y = perc,
                Weight = weight,
                Label = asset.Coin.Symbol
            };
            heatMapPoints.Add(hmPoint);
        }
        return index;
    }
    private static int AddHeatMapPointBreakEven(int index, ObservableCollection<HeatMapPoint> heatMapPoints, double sumMarketValue, AssetTotals? asset)
    {
        if (asset is null || asset.NetInvestment <= 0)
        {
            return index;
        }
        
        // only show a bubble when net Investment > 0
        var breakEvenPerc= 100 * ((asset.MarketValue - asset.NetInvestment)/asset.MarketValue) ;

        var weight = 100 * asset.MarketValue / sumMarketValue;

        if (!double.IsInfinity(breakEvenPerc))
        {
            var hmPoint = new HeatMapPoint
            {
                X = index += 1,
                // limit calculated distance to BE to a maximum of -1050. This because the Bubble map axis is limited as well
                Y = Math.Max(-1050,breakEvenPerc),
                RealYValue = breakEvenPerc,
                Weight = weight,
                Label = asset.Coin.Symbol
            };
            heatMapPoints.Add(hmPoint);
        }
        return index;
    }

    private int AddHeatMapPointEma(int index, ObservableCollection<HeatMapPoint> heatMapPoints, double sumMarketValue, AssetTotals? asset)
    {
        if (asset is null || asset.Coin is null)
        {
            return index;
        }

        var ema = _indicatorService.GetEma(asset.Coin);

        var distanceToEmaPerc = (100 * (asset.Coin.Price - ema) / ema);

        var weight = 100 * asset.MarketValue / sumMarketValue;

        if (!double.IsInfinity(distanceToEmaPerc))
        {
            var hmPoint = new HeatMapPoint
            {
                X = index += 1,
                Y = distanceToEmaPerc,
                Weight = weight,
                Label = asset.Coin.Symbol
            };
            heatMapPoints.Add(hmPoint);
        }
        return index;
    }

    public Portfolio GetPortfolio()
    {
        return CurrentPortfolio;
    }

    public void ClearCoinsList()
    {
        ListCoins = null;
        OnPropertyChanged(nameof(ListCoins));
    }

}
