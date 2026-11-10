using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CryptoPortfolioTracker.Services;

public interface IAssetService
{
    bool IsHidingZeroBalances { get; set; }
   
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsList();
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByAccountList(Account account);
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByNarrativeList(Narrative narrative);
    Task<ObservableCollection<AssetTotals>> UpdateListAssetTotals(Transaction transaction);
    Task CalculateAssetsTotalValues();
    Task CalculateYearlyCapitalFlow();
    double GetTotalsAssetsValue();
    double GetTotalsAssetsCostBase();
    double GetTotalsAssetsPnLPerc();
    long GetVisibleAssetsCount();
}

public partial class AssetService : ObservableObject, IAssetService
{
    private readonly Settings _appSettings;
    private readonly IAppState _appState;
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ICapitalFlowService _capitalFlowService;
    private ObservableCollection<AssetTotals>? ListAssetTotals { get; set; } = new();
    private double TotalAssetsValue { get; set; }
    private double TotalAssetsCostBase { get; set; }
    private double TotalAssetsPnLPerc { get; set; }
    private long VisibleAssetsCount { get; set; }
    private PortfolioContext Context => _appState.Context;

    public bool IsHidingZeroBalances { get; set;} = false;

    public AssetService(IAppState appState, Settings appSettings, IExchangeRateService exchangeRateService, ICapitalFlowService capitalFlowService)
    {
        _appSettings = appSettings;
        _appState = appState;
        _appState.ContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Context));
        };
        _exchangeRateService = exchangeRateService;
        _capitalFlowService = capitalFlowService;
        IsHidingZeroBalances = false;
    }

    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsList()
    {
        var getResult = await GetAssetTotalsFromContext(IsHidingZeroBalances);
        ListAssetTotals = getResult.Match(
           list => new ObservableCollection<AssetTotals>(list),
           err => new());
        VisibleAssetsCount = ListAssetTotals.Count();
        return ListAssetTotals;
    }

    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByAccountList(Account account)
    {
        if (account == null) return new ObservableCollection<AssetTotals>();

        var getResult = await GetAssetsByAccountFromContext(account.Id);
        ListAssetTotals = getResult.Match(
            list => new ObservableCollection<AssetTotals>(list),
            err => new());
        VisibleAssetsCount = ListAssetTotals.Count();
        return ListAssetTotals;
    }
    public async Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByNarrativeList(Narrative narrative)
    {
        if (narrative == null) return new ObservableCollection<AssetTotals>();

        var getResult = await GetAssetsByNarrativeFromContext(narrative.Id);
        ListAssetTotals = getResult.Match(
            list => new ObservableCollection<AssetTotals>(list),
            err => new());
        VisibleAssetsCount = ListAssetTotals.Count();
        return ListAssetTotals;
    }

    public double GetTotalsAssetsValue()
    {
        if (ListAssetTotals != null && ListAssetTotals.Any())
        {
            TotalAssetsValue = ListAssetTotals.Sum(x => x.MarketValue);
            TotalAssetsPnLPerc = TotalAssetsCostBase > 0
                ? 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase
                : 0;
        }
        else
        {
            TotalAssetsValue = 0;
        }

        return TotalAssetsValue;
    }
    public double GetTotalsAssetsCostBase()
    {
        if (ListAssetTotals != null && ListAssetTotals.Any())
        {
            TotalAssetsCostBase = ListAssetTotals.Sum(x => x.CostBase);
            TotalAssetsPnLPerc = TotalAssetsCostBase > 0 
                ? 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase 
                : 0;
        }
        else
        {
            TotalAssetsCostBase = 0;
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
    
    public long GetVisibleAssetsCount()
    {
        return VisibleAssetsCount;
    }

    public async Task CalculateAssetsTotalValues()
    {
        try
        {
            if (ListAssetTotals != null && ListAssetTotals.Count > 0 && ListAssetTotals[0].Coin.Symbol != "EXCEPTIONAL ERROR")
            {
                TotalAssetsValue = ListAssetTotals.Sum(x => x.MarketValue);
                TotalAssetsCostBase = ListAssetTotals.Sum(x => x.CostBase);
                TotalAssetsPnLPerc = 100 * (TotalAssetsValue - TotalAssetsCostBase) / TotalAssetsCostBase;
                await CalculateYearlyCapitalFlow();
            }
            else
            {
                TotalAssetsValue = 0;
                TotalAssetsCostBase = 0;
                TotalAssetsPnLPerc = 0;
                _capitalFlowService.CapitalFlow = new();
            }

        }
        catch (Exception ex)  
        {
            Debug.WriteLine(ex);
        }
    }

    public async Task<ObservableCollection<AssetTotals>> UpdateListAssetTotals(Transaction transaction)
    {
        ObservableCollection<AssetTotals> updated = new();

        if (ListAssetTotals is null || !ListAssetTotals.Any()) return new ObservableCollection<AssetTotals>();

        //for updating purpose of the View, the affected elements of the data source List has to be updated
        //*** First retrieve the coin(s) (max 2) affected by the transaction
        var coinsAffected = transaction.Mutations
            .Select(x => x.Asset.Coin)
            .GroupBy(x => x.Id) // or x.ApiId
            .Select(g => g.First())
            .ToList();

        // Check if one isn't in the assetsList yet, if so then add it.
        foreach (var coin in coinsAffected)
        {
            var assetAffected = ListAssetTotals.Where(x => x.Coin.Id == coin.Id).SingleOrDefault();
            if (assetAffected != null)
            {

                var index = ListAssetTotals.IndexOf(assetAffected);
                if (index >= 0)
                {
                    var editedAT = (await GetAssetTotalsByCoinFromContext(coin))
                        .Match(Succ: s => s, Fail: err => new AssetTotals());
                    if (editedAT.Coin is not null)
                    {
                        ListAssetTotals[index] = editedAT;
                        updated.Add(editedAT);
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
                    updated.Add(assetAffected);
                });
            }
        }
        return updated;
    }

    //public async Task CalculateYearlyCapitalFlow()
    //{
    //    var context = Context;

    //    // Calculate InFlow in Usd and Eur
    //    var yearlyInFlowListUsd = new List<YearlyFlow>();
    //    var yearlyInFlowListEur = new List<YearlyFlow>();

    //    var deposits = await context.Mutations
    //            .AsNoTracking()
    //            .Where(x => x.Type == TransactionKind.Deposit)
    //            .Include(t => t.Transaction)
    //            .Select(m => new
    //            {
    //                Date = m.Transaction.TimeStamp.Date,
    //                Qty = m.Qty,
    //                Price = m.Price
    //            })
    //            .ToListAsync();

    //    var result = deposits
    //        .GroupBy(x => x.Date.Year)
    //        .Select(grouped => new
    //        {
    //            Year = grouped.Key,
    //            ValueUsd = grouped.Sum(x => x.Qty * x.Price),
    //            ValueEur = grouped.Sum(x =>
    //            {
    //                var transactionDate = x.Date;
    //                var rate = _exchangeRateService.TryGetDailyRate(
    //                    DateOnly.FromDateTime(transactionDate),
    //                    out var dailyRate)
    //                    ? dailyRate
    //                    : 1;

    //                return (x.Qty * x.Price) / rate;
    //            })
    //        })
    //        .OrderBy(x => x.Year)
    //        .ToList();

    //    foreach (var deposit in result)
    //    {
    //        var flowUsd = new YearlyFlow();
    //        var flowEur = new YearlyFlow();
    //        flowUsd.Year = deposit.Year;
    //        flowUsd.Value = deposit.ValueUsd;
    //        flowEur.Year = flowUsd.Year;
    //        flowEur.Value = deposit.ValueEur;
    //        yearlyInFlowListUsd.Add(flowUsd);
    //        yearlyInFlowListEur.Add(flowEur);
    //    }

    //    _capitalFlowService.CapitalFlow.InFlowUsdYearly = yearlyInFlowListUsd;
    //    _capitalFlowService.CapitalFlow.InFlowEurYearly = yearlyInFlowListEur;

    //    // Calculate OutFlow in Usd and Eur
    //    var yearlyOutFlowListUsd = new List<YearlyFlow>();
    //    var yearlyOutFlowListEur = new List<YearlyFlow>();

    //    var withdrawals = await context.Mutations
    //            .AsNoTracking()
    //            .Where(x => x.Type == TransactionKind.Withdraw)
    //            .Include(t => t.Transaction)
    //            .Select(m => new
    //            {
    //                Date = m.Transaction.TimeStamp.Date,
    //                Qty = m.Qty,
    //                Price = m.Price
    //            })
    //            .ToListAsync();

    //    result = withdrawals
    //        .GroupBy(x => x.Date.Year)
    //        .Select(grouped => new
    //        {
    //            Year = grouped.Key,
    //            ValueUsd = grouped.Sum(x => x.Qty * x.Price),
    //            ValueEur = grouped.Sum(x =>
    //            {
    //                var transactionDate = x.Date;
    //                var rate = _exchangeRateService.TryGetDailyRate(
    //                    DateOnly.FromDateTime(transactionDate),
    //                    out var dailyRate)
    //                    ? dailyRate
    //                    : 1;

    //                return (x.Qty * x.Price) / rate;
    //            })
    //        })
    //        .OrderBy(x => x.Year)
    //        .ToList();

    //    foreach (var withdrawal in result)
    //    {
    //        var flowUsd = new YearlyFlow();
    //        var flowEur = new YearlyFlow();
    //        flowUsd.Year = withdrawal.Year;
    //        flowUsd.Value = withdrawal.ValueUsd;
    //        flowEur.Year = flowUsd.Year;
    //        flowEur.Value = withdrawal.ValueEur;
    //        yearlyOutFlowListUsd.Add(flowUsd);
    //        yearlyOutFlowListEur.Add(flowEur);
    //    }

    //    _capitalFlowService.CapitalFlow.OutFlowUsdYearly = yearlyOutFlowListUsd;
    //    _capitalFlowService.CapitalFlow.OutFlowEurYearly = yearlyOutFlowListEur;

    //}

    public async Task CalculateYearlyCapitalFlow()
    {
        var (depositUsd, depositEur) = await GetYearlyFlowsAsync(TransactionKind.Deposit);
        var (withdrawUsd, withdrawEur) = await GetYearlyFlowsAsync(TransactionKind.Withdraw);

        _capitalFlowService.CapitalFlow.InFlowUsdYearly = depositUsd;
        _capitalFlowService.CapitalFlow.InFlowEurYearly = depositEur;

        _capitalFlowService.CapitalFlow.OutFlowUsdYearly = withdrawUsd;
        _capitalFlowService.CapitalFlow.OutFlowEurYearly = withdrawEur;
    }

    private async Task<(List<YearlyFlow> Usd, List<YearlyFlow> Eur)> GetYearlyFlowsAsync(TransactionKind kind)
    {
        var context = Context;

        var rows = await context.Mutations
            .AsNoTracking()
            .Where(x => x.Type == kind)
            .Include(t => t.Transaction)
            .Select(m => new
            {
                Date = m.Transaction.TimeStamp.Date,
                Qty = m.Qty,
                Price = m.Price
            })
            .ToListAsync();

        var grouped = rows
            .GroupBy(x => x.Date.Year)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Year = g.Key,
                ValueUsd = g.Sum(x => x.Qty * x.Price),
                ValueEur = g.Sum(x =>
                {
                    var rate = _exchangeRateService.TryGetDailyRate(
                        DateOnly.FromDateTime(x.Date),
                        out var dailyRate)
                        ? dailyRate
                        : 1;

                    return (x.Qty * x.Price) / rate;
                })
            })
            .ToList();

        var usd = grouped
            .Select(x => new YearlyFlow { Year = x.Year, Value = x.ValueUsd })
            .ToList();

        var eur = grouped
            .Select(x => new YearlyFlow { Year = x.Year, Value = x.ValueEur })
            .ToList();

        return (usd, eur);
    }


    private async Task<Result<List<AssetTotals>>> GetAssetsByAccountFromContext(int accountId)
    {
        var context = Context;
        List<AssetTotals> assetsTotals;
        if (accountId <= 0) { return new List<AssetTotals>(); }
        try
        {
            if (IsHidingZeroBalances)
            {
                assetsTotals = await context.Assets
                    .AsNoTracking()
                    .Where(c => c.Account.Id == accountId)
                    .Include(x => x.Coin)
                    .GroupBy(asset => asset.Coin)
                    .Select(assetGroup => new AssetTotals
                    {
                        Qty = assetGroup.Sum(x => x.Qty),
                        CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                        Coin = assetGroup.Key
                    })
                    .Where( x => x.Qty > 0)
                    .ToListAsync();
            }
            else
            {
                assetsTotals = await context.Assets
                    .AsNoTracking()
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
            }

            await CalculateNetInvestmentsByAccountAsync(assetsTotals, accountId);
        }
        catch (Exception ex)
        {
            return new Result<List<AssetTotals>>(ex);
        }
        return assetsTotals;
    }

    private async Task<Result<List<AssetTotals>>> GetAssetsByNarrativeFromContext(int narrativeId)
    {
        var context = Context;
        List<AssetTotals> assetsTotals = null;
        if (narrativeId <= 0) { return new List<AssetTotals>(); }
        try
        {
            if (IsHidingZeroBalances)
            {
                assetsTotals = await context.Narratives
                    .AsNoTracking()
                    .Where(n => n.Id == narrativeId)
                    .SelectMany(n => n.Coins)
                    .Where(c => c.IsAsset)
                    .Select(c => new AssetTotals
                    {
                        Coin = c,
                        Qty = c.Assets.Sum(a => a.Qty),
                        CostBase = c.Assets.Sum(a => a.AverageCostPrice * a.Qty)
                    })
                    .Where(x => x.Qty > 0)
                    .ToListAsync();
            }
            else
            {
                assetsTotals = await context.Narratives
                .AsNoTracking()
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
            }
        }
        catch (Exception ex)
        {
            return new Result<List<AssetTotals>>(ex);
        }
        return assetsTotals;
    }

    
    private async Task<Result<List<AssetTotals>>> GetAssetTotalsFromContext(bool excludeZeroBalance)
    {
        var context = Context;
        List<AssetTotals> assetsTotals = null;
        try
        {
            // Select clause after a GroupBy is seen as a 2nd projection causing in 
            //the 'ThenInclude' to be ignored'/lost. For that reason the query is in 2 steps.
            var groupedAssets = await context.Assets
                .Include(x => x.Coin)
                    .ThenInclude(y => y.PriceLevels)
                .GroupBy(asset => asset.Coin)
                .ToListAsync();

            if (IsHidingZeroBalances)
            {
                assetsTotals = groupedAssets
                    .Select(groupedAsset => new AssetTotals
                    {
                        Coin = groupedAsset.Key,
                        Qty = groupedAsset.Sum(x => x.Qty),
                        CostBase = groupedAsset.Sum(x => x.AverageCostPrice * x.Qty)
                    })
                    .Where(x => x.Qty > 0)
                    .ToList();
            }
            else
            {
                assetsTotals = groupedAssets
                    .Select(groupedAsset => new AssetTotals
                    {
                        Coin = groupedAsset.Key,
                        Qty = groupedAsset.Sum(x => x.Qty),
                        CostBase = groupedAsset.Sum(x => x.AverageCostPrice * x.Qty)
                    })
                    .ToList();
            }

            await CalculateNetInvestmentsAsync(assetsTotals);

            VisibleAssetsCount = assetsTotals is not null ? assetsTotals.Count : 0;
            
        }
       
        catch (Exception ex)
        {
            return new Result<List<AssetTotals>>(ex);
        }
        assetsTotals ??= new List<AssetTotals>();

        return assetsTotals;
    }

    
    
    private async Task<Result<AssetTotals>> GetAssetTotalsByCoinFromContext(Coin coin)
    {
        var context = Context;
        if (coin == null) { return new AssetTotals(); }
        AssetTotals assetTotal = null;
        try
        {
            var groupedAssets = await context.Assets
                .Where(c => c.Coin.Id == coin.Id)
               .Include(x => x.Coin)
               .ThenInclude(y => y.PriceLevels)
               .GroupBy(asset => asset.Coin)
               .ToListAsync();

            assetTotal = groupedAssets.Select(assetGroup => new AssetTotals
            {
                Qty = assetGroup.Sum(x => x.Qty),
                CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                Coin = assetGroup.Key
            }).Single();

            await CalculateNetInvestmentAsync(assetTotal);
        }
        catch (Exception ex)
        {
            return new Result<AssetTotals>(ex);
        }
        return assetTotal;
    }

    //private void CalculateNetInvestment(AssetTotals assetTotal)
    //{
    //    var context = Context;
    //    var sumBuy = context.Mutations
    //        .AsNoTracking()
    //        .Where(m => m.Direction == MutationDirection.In
    //            && m.Asset.Coin.Id == assetTotal.Coin.Id)
    //        .Sum(s => s.Qty * s.Price);

    //    var sumSell = context.Mutations
    //        .AsNoTracking()
    //        .Where(m => m.Direction == MutationDirection.Out
    //            && m.Asset.Coin.Id == assetTotal.Coin.Id)
    //        .Sum(s => s.Qty * s.Price);

    //    var exchRate = _appSettings.Currency == "eur" ? _capitalFlowService.CapitalFlowExchangeRateEurUsd : 1;

    //    assetTotal.NetInvestment = (sumBuy - sumSell)/exchRate;

    //}

    private async Task CalculateNetInvestmentAsync(AssetTotals assetTotal)
    {
        var exchangeRate = _appSettings.Currency == "eur"
            ? _capitalFlowService.CapitalFlowExchangeRateEurUsd
            : 1;

        var result = await Context.Mutations
            .AsNoTracking()
            .Where(m => m.Asset.Coin.Id == assetTotal.Coin.Id)
            .GroupBy(m => m.Asset.Coin.Id)
            .Select(g => new
            {
                Buy = g
                    .Where(m => m.Direction == MutationDirection.In)
                    .Sum(m => m.Qty * m.Price),
                Sell = g
                    .Where(m => m.Direction == MutationDirection.Out)
                    .Sum(m => m.Qty * m.Price)
            })
            .SingleOrDefaultAsync();

        assetTotal.NetInvestment = result is null
            ? 0
            : (result.Buy - result.Sell) / exchangeRate;
    }

    private async Task CalculateNetInvestmentsAsync(List<AssetTotals> assetsTotals)
    {
        var exchangeRate = _appSettings.Currency == "eur"
            ? _capitalFlowService.CapitalFlowExchangeRateEurUsd
            : 1;

        var netByCoin = await Context.Mutations
            .AsNoTracking()
            .GroupBy(m => m.Asset.Coin.Id)
            .Select(g => new
            {
                CoinId = g.Key,
                Buy = g
                    .Where(m => m.Direction == MutationDirection.In)
                    .Sum(m => m.Qty * m.Price),
                Sell = g
                    .Where(m => m.Direction == MutationDirection.Out)
                    .Sum(m => m.Qty * m.Price)
            })
            .ToDictionaryAsync(x => x.CoinId);

        foreach (var assetTotal in assetsTotals)
        {
            if (netByCoin.TryGetValue(assetTotal.Coin.Id, out var totals))
            {
                assetTotal.NetInvestment = (totals.Buy - totals.Sell) / exchangeRate;
            }
            else
            {
                assetTotal.NetInvestment = 0;
            }
        }
    }
    //private void CalculateNetInvestments(List<AssetTotals> assetsTotals)
    //{
    //    var exchRate = _appSettings.Currency == "eur" ? _capitalFlowService.CapitalFlowExchangeRateEurUsd : 1;
    //    var context = Context;
    //    foreach (var assetTotal in assetsTotals)
    //    {
    //        var sumBuy = context.Mutations
    //            .AsNoTracking()
    //            .Where(m => m.Direction == MutationDirection.In
    //                && m.Asset.Coin.Id == assetTotal.Coin.Id)
    //            .Sum(s => s.Qty * s.Price);

    //        var sumSell = context.Mutations
    //            .AsNoTracking()
    //            .Where(m => m.Direction == MutationDirection.Out
    //                && m.Asset.Coin.Id == assetTotal.Coin.Id)
    //            .Sum(s => s.Qty * s.Price);

    //        assetTotal.NetInvestment = (sumBuy - sumSell) / exchRate;
    //    }
    //}

    //private void CalculateNetInvestmentsByAccount(List<AssetTotals> assetsTotals, int accountId)
    //{
    //    var exchRate = _appSettings.Currency == "eur" ? _capitalFlowService.CapitalFlowExchangeRateEurUsd : 1;
    //    var context = Context;
    //    foreach (var assetTotal in assetsTotals)
    //    {
    //        var sumBuy = context.Mutations
    //            .AsNoTracking()
    //            .Where(m => m.Direction == MutationDirection.In
    //                && m.Asset.Coin.Id == assetTotal.Coin.Id
    //                && m.Asset.Account.Id == accountId)
    //            .Sum(s => s.Qty * s.Price);

    //        var sumSell = context.Mutations
    //            .AsNoTracking()
    //            .Where(m => m.Direction == MutationDirection.Out
    //                && m.Asset.Coin.Id == assetTotal.Coin.Id
    //                && m.Asset.Account.Id == accountId)
    //            .Sum(s => s.Qty * s.Price);


    //        assetTotal.NetInvestment = (sumBuy - sumSell) / exchRate;
    //    }
    //}

    private async Task CalculateNetInvestmentsByAccountAsync(List<AssetTotals> assetsTotals, int accountId)
    {
        var exchangeRate = _appSettings.Currency == "eur"
            ? _capitalFlowService.CapitalFlowExchangeRateEurUsd
            : 1;

        var netByCoin = await Context.Mutations
            .AsNoTracking()
            .Where(m => m.Asset.Account.Id == accountId)
            .GroupBy(m => m.Asset.Coin.Id)
            .Select(g => new
            {
                CoinId = g.Key,
                Buy = g
                    .Where(m => m.Direction == MutationDirection.In)
                    .Sum(m => m.Qty * m.Price),
                Sell = g
                    .Where(m => m.Direction == MutationDirection.Out)
                    .Sum(m => m.Qty * m.Price)
            })
            .ToDictionaryAsync(x => x.CoinId);

        foreach (var assetTotal in assetsTotals)
        {
            if (netByCoin.TryGetValue(assetTotal.Coin.Id, out var totals))
            {
                assetTotal.NetInvestment = (totals.Buy - totals.Sell) / exchangeRate;
            }
            else
            {
                assetTotal.NetInvestment = 0;
            }
        }
    }

}