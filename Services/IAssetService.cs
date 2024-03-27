using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IAssetService
    {
        public Task<Result<List<AssetTotals>>> GetAssetTotals();
        public Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin);
        public Task<Result<List<AssetAccount>>> GetAccountsByAsset(int coinId);
        public Task<Result<AssetAccount>> GetAccountByAsset(int assetId);
        
        public Task<Result<List<AssetTransaction>>> GetTransactionsByAsset(int assetId);
        //public Task<Result<AssetTransaction>> GetTransactionById(int transactionId);
    }
}

