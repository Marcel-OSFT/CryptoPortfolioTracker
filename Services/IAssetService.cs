using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface IAssetService
{
    //bool IsSortingAfterUpdateEnabled { get; set; }
    bool IsHidingZeroBalances { get; set; }

    //public Task<Result<List<AssetTotals>>> GetAssetTotals();
    // public Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin);
    // public Task<Result<List<AssetAccount>>> GetAccountsByAsset(int coinId);
    //  public Task<Result<AssetAccount>> GetAccountByAsset(int assetId);
    //public Task<Result<List<Transaction>>> GetTransactionsByAsset(int assetId);
   
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
}

