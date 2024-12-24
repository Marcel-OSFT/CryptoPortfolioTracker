using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface IAssetService
{
    bool IsHidingZeroBalances { get; set; }
    public Task<Result<AssetTotals>> GetAssetTotalsByCoinAndAccountFromContext(Coin coin, Account account);
    void UpdatePricesAssetTotals(Coin coin, double oldPrice, double? newPrice);
    Task CalculateAssetsTotalValues();
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsList();
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByAccountList(Account account);
    void SortList(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc);
    Task UpdateListAssetTotals(Transaction transaction);
    double GetTotalsAssetsValue();
    double GetTotalsAssetsCostBase();
    double GetTotalsAssetsPnLPerc();
    Task<double> GetInFlow();
    Task<double> GetOutFlow();
    void SortList();
    void ClearAssetTotalsList();
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsList(SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc);
    Task<ObservableCollection<AssetTotals>> PopulateAssetTotalsByAccountList(Account account, SortingOrder sortingOrder, Func<AssetTotals, object> sortFunc);
    void ReloadValues();
    //ObservableCollection<AssetTotals> GetAssetTotalsList();
}

