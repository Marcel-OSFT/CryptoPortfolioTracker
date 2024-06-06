using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface IAssetService
{
    public Task<Result<List<AssetTotals>>> GetAssetTotals();
    public Task<Double> GetInFlow();
    public Task<Double> GetOutFlow();

    public Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin);
    public Task<Result<List<AssetAccount>>> GetAccountsByAsset(int coinId);
    public Task<Result<AssetAccount>> GetAccountByAsset(int assetId);
    public Task<Result<AssetTotals>> GetAssetTotalsByCoinAndAccount(Coin coin, Account account);
    public Task<Result<List<Transaction>>> GetTransactionsByAsset(int assetId);
}

