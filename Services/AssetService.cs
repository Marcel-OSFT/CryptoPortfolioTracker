using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Windows.Gaming.Input.ForceFeedback;

namespace CryptoPortfolioTracker.Services;

public partial class AssetService : ObservableObject, IAssetService
{
    private readonly PortfolioContext context;
    [ObservableProperty] private static ObservableCollection<AssetTotals>? listAssetTotals;
    [ObservableProperty] private double totalAssetsValue;
    [ObservableProperty] private double totalAssetsCostBase;
    [ObservableProperty] private double totalAssetsPnLPerc;
    [ObservableProperty] private double inFlow;
    [ObservableProperty] private double outFlow;
    private SortingOrder currentSortingOrder;
    private Func<AssetTotals, object> currentSortFunc;
    [ObservableProperty] public bool isHidingZeroBalances;
    [ObservableProperty] public long visibleAssetsCount;
    //private long hiddenAssetsCount;

    partial void OnIsHidingZeroBalancesChanged(bool value)
    {
        if (ListAssetTotals == null)
        {
            return;
        }

        VisibleAssetsCount = ListAssetTotals.Count;
        if (value)
        {
            var itemsToHide = ListAssetTotals.Where(x => x.MarketValue <= 0).ToList();
            foreach (var item in itemsToHide)
            {
                item.IsHidden = true; ;
            }
            if (itemsToHide != null)
            {
                VisibleAssetsCount -= itemsToHide.Count;
                //hiddenAssetsCount = itemsToHide.Count;
            }
        }
        else
        {
            foreach (var item in ListAssetTotals)
            {
                item.IsHidden = false;
                // hiddenAssetsCount = 0;
            }
        }
    }

    public AssetService(PortfolioContext portfolioContext)
    {
        context = portfolioContext;
        currentSortFunc = x => x.MarketValue;
        currentSortingOrder = SortingOrder.Descending;

        IsHidingZeroBalances = false;
    }

    public void ReloadValues()
    {
        var copyList = ListAssetTotals;
        ListAssetTotals = null;
        ListAssetTotals = copyList;
       
        OnPropertyChanged(nameof(TotalAssetsValue));
        OnPropertyChanged(nameof(InFlow));
        OnPropertyChanged(nameof(OutFlow));
    }


    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsList()
    {
        var getAssetTotalsResult = await GetAssetTotalsFromContext();
        getAssetTotalsResult.IfSucc(list =>
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new(list.OrderBy(currentSortFunc));
            }
            else
            {
                ListAssetTotals = new(list.OrderByDescending(currentSortFunc));
            }
           
            //VisibleAssetsCount = IsHidingZeroBalances ? ListAssetTotals.Count - ListAssetTotals.Where(x => x.MarketValue <= 0).ToList().Count : ListAssetTotals.Count;
        });
        getAssetTotalsResult.IfFail(err => ListAssetTotals = new());
        return ListAssetTotals;
    }

    //public ObservableCollection<AssetTotals> GetAssetTotalsList()
    //{
    //    return ListAssetTotals;
    //}




    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsList(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    {
        var getAssetTotalsResult = await GetAssetTotalsFromContext();
        getAssetTotalsResult.IfSucc(list =>
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new(list.OrderBy(sortFunc));
            }
            else
            {
                ListAssetTotals = new(list.OrderByDescending(sortFunc));
            }
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;
           //VisibleAssetsCount = IsHidingZeroBalances ? ListAssetTotals.Count - ListAssetTotals.Where(x => x.MarketValue <= 0).ToList().Count : ListAssetTotals.Count;

        });
        getAssetTotalsResult.IfFail(err => ListAssetTotals = new());
        return ListAssetTotals;
    }
    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByAccountList(Account account)
    {
        var getResult = await GetAssetsByAccountFromContext(account.Id);
        getResult.IfSucc(list =>
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new(list.OrderBy(currentSortFunc));
            }
            else
            {
                ListAssetTotals = new(list.OrderByDescending(currentSortFunc));
            }
        });
        getResult.IfFail(err => ListAssetTotals = new());
        return ListAssetTotals;
    }
    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByNarrativeList(Narrative narrative)
    {
        var getResult = await GetAssetsByNarrativeFromContext(narrative.Id);
        getResult.IfSucc(list =>
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new(list.OrderBy(currentSortFunc));
            }
            else
            {
                ListAssetTotals = new(list.OrderByDescending(currentSortFunc));
            }
        });
        getResult.IfFail(err => ListAssetTotals = new());
        return ListAssetTotals;
    }
    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByAccountList(Account account, SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    {
        var getResult = await GetAssetsByAccountFromContext(account.Id);
        getResult.IfSucc(list =>
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new(list.OrderBy(sortFunc));
            }
            else
            {
                ListAssetTotals = new(list.OrderByDescending(sortFunc));
            }
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;
        });
        getResult.IfFail(err => ListAssetTotals = new());
        return ListAssetTotals;
    }
    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByNarrativeList(Narrative narrative, SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    {
        var getResult = await GetAssetsByNarrativeFromContext(narrative.Id);
        getResult.IfSucc(list =>
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new(list.OrderBy(sortFunc));
            }
            else
            {
                ListAssetTotals = new(list.OrderByDescending(sortFunc));
            }
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;
        });
        getResult.IfFail(err => ListAssetTotals = new());
        return ListAssetTotals;
    }

    public void ClearAssetTotalsList()
    {
        if (ListAssetTotals is not null)
        {
            ListAssetTotals.Clear();
        }
    }
    public double GetTotalsAssetsValue()
    {
        if (ListAssetTotals != null && ListAssetTotals.Count > 0)
        {
            TotalAssetsValue = 0; // force view update
            TotalAssetsValue = ListAssetTotals.Sum(x => x.MarketValue);
            TotalAssetsPnLPerc = TotalAssetsCostBase > 0
                ? 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase
                : 0;
        }
        return TotalAssetsValue;
    }
    public double GetTotalsAssetsCostBase()
    {
        if (ListAssetTotals != null && ListAssetTotals.Count > 0)
        {
            TotalAssetsCostBase = ListAssetTotals.Sum(x => x.CostBase);
            TotalAssetsPnLPerc = TotalAssetsCostBase > 0 
                ? 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase 
                : 0;
        }
        return TotalAssetsCostBase;
    }
    public double GetTotalsAssetsPnLPerc()
    {
        TotalAssetsPnLPerc = 0; // force view update
        TotalAssetsPnLPerc = TotalAssetsCostBase > 0
            ? 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase
            : 0;
        
        return TotalAssetsPnLPerc;
    }
    public async Task<double> GetInFlow()
    {
        InFlow = 0; // force view update
        InFlow = await CalculateInFlow();
        return InFlow;
    }
    public async Task<double> GetOutFlow()
    {
        OutFlow = 0; // force view update
        OutFlow = await CalculateOutFlow();
        return OutFlow;
    }
    public async Task CalculateAssetsTotalValues()
    {
        if (ListAssetTotals != null && ListAssetTotals.Count > 0 && ListAssetTotals[0].Coin.Symbol != "EXCEPTIONAL ERROR")
        {
            TotalAssetsValue = ListAssetTotals.Sum(x => x.MarketValue);
            TotalAssetsCostBase = ListAssetTotals.Sum(x => x.CostBase);
            TotalAssetsPnLPerc = 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase;
        }
        InFlow = await CalculateInFlow();
        OutFlow = await CalculateOutFlow();
    }
    public void UpdatePricesAssetTotals(Coin coin, double oldPrice, double? newPrice)
    {
        var asset = ListAssetTotals?.Where(a => a.Coin.Id == coin.Id).SingleOrDefault();
        if (asset != null && ListAssetTotals != null && oldPrice != newPrice)
        {
            //Logger.Information("Updating {0} {1} => {2}", coin.Name, oldPrice, newPrice);
            var index = -1;
            for (var i = 0; i < ListAssetTotals.Count; i++)
            {
                if (ListAssetTotals[i].Coin.Id == asset.Coin.Id)
                {
                    index = i;
                    break;
                }
            }
            ListAssetTotals[index].Coin = coin;
            ListAssetTotals[index].MarketValue = ListAssetTotals[index].Qty * coin.Price;
        }
        //apply / refresh sorting
        //if (IsSortingAfterUpdateEnabled)
        //{
        //    SortList(currentSortingOrder, currentSortFunc);
        //}

    }
    private async Task<Result<List<AssetTotals>>> GetAssetsByAccountFromContext(int accountId)
    {
        List<AssetTotals> assetsTotals;
        if (accountId <= 0) { return new List<AssetTotals>(); }
        try
        {
            assetsTotals = await context.Assets
            .Where(c => c.Account.Id == accountId)
            .Include(x => x.Coin)
            .GroupBy(asset => asset.Coin)
            .Select(assetGroup => new AssetTotals
            {
                Qty = assetGroup.Sum(x => x.Qty),
                CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                Coin = assetGroup.Key
            })
            .ToListAsync();

            GetNetInvestmentsByAccount(assetsTotals, accountId);

            VisibleAssetsCount = assetsTotals is not null ? assetsTotals.Count : 0;
            if (IsHidingZeroBalances)
            {
                var assetsWithZero = assetsTotals.Where(x => x.MarketValue <= 0);
                if (assetsWithZero is not null)
                {
                    foreach (var asset in assetsWithZero)
                    {
                        asset.IsHidden = true;
                    }
                    VisibleAssetsCount -= assetsWithZero.ToList().Count;
                }
                
            }
        }
        catch (Exception ex)
        {
            return new Result<List<AssetTotals>>(ex);
        }
        return assetsTotals;
    }

    private async Task<Result<List<AssetTotals>>> GetAssetsByNarrativeFromContext(int narrativeId)
    {
        List<AssetTotals> assetsTotals = null;
        if (narrativeId <= 0) { return new List<AssetTotals>(); }
        try
        {
            assetsTotals = await context.Narratives
                .Where(n => n.Id == narrativeId)
                .SelectMany(n => n.Coins)
                .Where(c => c.IsAsset)
                .Select(c => new AssetTotals
                {
                    Coin = c,
                    Qty = c.Assets.Sum(a => a.Qty),
                    CostBase = c.Assets.Sum(a => a.AverageCostPrice * a.Qty)
                })
                .ToListAsync();

            VisibleAssetsCount = assetsTotals is not null ? assetsTotals.Count : 0;
            if (IsHidingZeroBalances)
            {
                var assetsWithZero = assetsTotals.Where(x => x.MarketValue <= 0);
                if (assetsWithZero is not null)
                {
                    foreach (var asset in assetsWithZero)
                    {
                        asset.IsHidden = true;
                    }
                    VisibleAssetsCount -= assetsWithZero.ToList().Count;
                }
            }
        }
        catch (Exception ex)
        {
            return new Result<List<AssetTotals>>(ex);
        }
        return assetsTotals;
    }

    public void SortList(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc)
    {
        if (ListAssetTotals is not null)
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new ObservableCollection<AssetTotals>(ListAssetTotals.OrderBy(sortFunc));
            }
            else
            {
                ListAssetTotals = new ObservableCollection<AssetTotals>(ListAssetTotals.OrderByDescending(sortFunc));
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
        if (ListAssetTotals is not null)
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListAssetTotals = new ObservableCollection<AssetTotals>(ListAssetTotals.OrderBy(currentSortFunc));
            }
            else
            {
                ListAssetTotals = new ObservableCollection<AssetTotals>(ListAssetTotals.OrderByDescending(currentSortFunc));
            }
        }
    }
    public async Task UpdateListAssetTotals(Transaction transaction)
    {
        if (ListAssetTotals is null)
        {
            return;
        }
        //for updating purpose of the View, the affected elements of the data source List has to be updated
        //*** First retrieve the coin(s) (max 2) affected by the transaction
        var coinsAffected = transaction.Mutations.Select(x => x.Asset.Coin).Distinct().ToList();

        // Check if one isn't in the assetsList yet, if so then add it.
        foreach (var coin in coinsAffected)
        {
            var assetAffected = ListAssetTotals.Where(x => x.Coin.Id == coin.Id).SingleOrDefault();
            if (assetAffected != null)
            {
                var index = -1;
                for (var i = 0; i < ListAssetTotals.Count; i++)
                {
                    if (ListAssetTotals[i].Coin.Id == assetAffected.Coin.Id)
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                {
                    var editedAT = (await GetAssetTotalsByCoinFromContext(coin)).Match(Succ: s => s, Fail: err => new AssetTotals());
                    if (editedAT.Coin is not null)
                    {
                        ListAssetTotals[index] = editedAT;
                    }
                }
            }
            else //assetAffected == null
            {
                assetAffected = new AssetTotals();

                (await GetAssetTotalsByCoinFromContext(coin)).IfSucc(s =>
                {
                    assetAffected = s;
                    ListAssetTotals.Add(assetAffected);
                });
            }
        }
    }
    private async Task<Result<List<AssetTotals>>> GetAssetTotalsFromContext()
    {
        List<AssetTotals> assetsTotals;
        try
        {
            assetsTotals = await context.Assets
            .Include(x => x.Coin)
            .ThenInclude(y => y.PriceLevels)
            .GroupBy(asset => asset.Coin)
            .Select(assetGroup => new AssetTotals
            {
                Qty = assetGroup.Sum(x => x.Qty),
                CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                Coin = assetGroup.Key,
            })
            .ToListAsync();

            GetNetInvestments(assetsTotals);


            VisibleAssetsCount = assetsTotals is not null ? assetsTotals.Count : 0;
            if (IsHidingZeroBalances)
            {
                var assetsWithZero = assetsTotals.Where(x => x.MarketValue <= 0);
                if (assetsWithZero is not null)
                {
                    foreach (var asset in assetsWithZero)
                    {
                        asset.IsHidden = true;
                    }
                    VisibleAssetsCount -= assetsWithZero.ToList().Count;
                }

            }
        }
       
        catch (Exception ex)
        {
            return new Result<List<AssetTotals>>(ex);
        }
        assetsTotals ??= new List<AssetTotals>();

        return assetsTotals;
    }
    private async Task<Double> CalculateInFlow()
    {
        double inFlow = 0;
        try
        {
            var deposits = await context.Mutations
            .Where(m => m.Type == TransactionKind.Deposit)
            .ToListAsync();
            
            inFlow = deposits.Sum(s => s.Qty * s.Price);
        }
        catch (Exception)
        {
            return -1;
        }

        return inFlow;
    }
    private async Task<Double> CalculateOutFlow()
    {
        double outFlow = 0;
        try
        {
            var withdraws = await context.Mutations
            .Where(m => m.Type == TransactionKind.Withdraw)
            .ToListAsync();
            
            outFlow = withdraws.Sum(s => s.Qty * s.Price);

        }
        catch (Exception)
        {
            return -1;
        }

        return outFlow;
    }
    private async Task<Result<AssetTotals>> GetAssetTotalsByCoinFromContext(Coin coin)
    {
        if (coin == null) { return new AssetTotals(); }
        AssetTotals assetTotal;
        try
        {
            assetTotal = await context.Assets
           .Where(c => c.Coin.Id == coin.Id)
           .Include(x => x.Coin)
           .GroupBy(asset => asset.Coin)
           .Select(assetGroup => new AssetTotals
           {
               Qty = assetGroup.Sum(x => x.Qty),
               CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
               Coin = assetGroup.Key
           })
           .SingleAsync();

            GetNetInvestment(assetTotal);
        }
        catch (Exception ex)
        {
            return new Result<AssetTotals>(ex);
        }
        return assetTotal;
    }
    public async Task<Result<AssetTotals>> GetAssetTotalsByCoinAndAccountFromContext(Coin coin, Account account)
    {
        if (coin == null || account == null) { return new AssetTotals(); }
        AssetTotals assetTotals;
        try
        {
            assetTotals = await context.Assets
           .Include(x => x.Coin)
           .Include(y => y.Account)
           .Where(c => c.Coin.Id == coin.Id && c.Account.Id == account.Id)
           .GroupBy(asset => asset.Coin)
           .Select(assetGroup => new AssetTotals
           {
               Qty = assetGroup.Sum(x => x.Qty),
               CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
               Coin = assetGroup.Key
           })
           .SingleAsync();

            GetNetInvestmentByAccount(assetTotals, account.Id);

        }
        catch (Exception ex)
        {
            return new Result<AssetTotals>(ex);
        }
        return assetTotals;
    }

    private void GetNetInvestment(AssetTotals assetTotal)
    {
        var sumBuy = context.Mutations
            .Where(m => m.Direction == MutationDirection.In
                && m.Asset.Coin.Id == assetTotal.Coin.Id)
            .Sum(s => s.Qty * s.Price);

        var sumSell = context.Mutations
            .Where(m => m.Direction == MutationDirection.Out
                && m.Asset.Coin.Id == assetTotal.Coin.Id)
            .Sum(s => s.Qty * s.Price);

        assetTotal.NetInvestment = sumBuy - sumSell;

    }

    private void GetNetInvestmentByAccount(AssetTotals assetTotal, int accountId)
    {
        var sumBuy = context.Mutations
            .Where(m => m.Direction == MutationDirection.In
                && m.Asset.Coin.Id == assetTotal.Coin.Id
                && m.Asset.Account.Id == accountId)
            .Sum(s => s.Qty * s.Price);

        var sumSell = context.Mutations
            .Where(m => m.Direction == MutationDirection.Out
                && m.Asset.Coin.Id == assetTotal.Coin.Id
                && m.Asset.Account.Id == accountId)
            .Sum(s => s.Qty * s.Price);

        assetTotal.NetInvestment = sumBuy - sumSell;

    }

    private void GetNetInvestments(List<AssetTotals> assetsTotals)
    {

        foreach (var assetTotal in assetsTotals)
        {
            var sumBuy = context.Mutations
            .Where(m => m.Direction == MutationDirection.In
                && m.Asset.Coin.Id == assetTotal.Coin.Id)
            .Sum(s => s.Qty * s.Price);

            var sumSell = context.Mutations
                .Where(m => m.Direction == MutationDirection.Out
                    && m.Asset.Coin.Id == assetTotal.Coin.Id)
                .Sum(s => s.Qty * s.Price);

            assetTotal.NetInvestment = sumBuy - sumSell;
        }
    }

    private void GetNetInvestmentsByAccount(List<AssetTotals> assetsTotals, int accountId)
    {

        foreach (var assetTotal in assetsTotals)
        {
            var sumBuy = context.Mutations
            .Where(m => m.Direction == MutationDirection.In
                && m.Asset.Coin.Id == assetTotal.Coin.Id
                && m.Asset.Account.Id == accountId)
            .Sum(s => s.Qty * s.Price);

            var sumSell = context.Mutations
                .Where(m => m.Direction == MutationDirection.Out
                    && m.Asset.Coin.Id == assetTotal.Coin.Id
                    && m.Asset.Account.Id == accountId)
                .Sum(s => s.Qty * s.Price);

            assetTotal.NetInvestment = sumBuy - sumSell;
        }

    }

    


}