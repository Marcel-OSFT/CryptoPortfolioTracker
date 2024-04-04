using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IAssetService
    {
        public Task<Result<List<AssetTotals>>> GetAssetTotals();
        public Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin);
        public Task<Result<List<AssetAccount>>> GetAccountsByAsset(int coinId);
        public Task<Result<AssetAccount>> GetAccountByAsset(int assetId);
        public Task<Result<AssetTotals>> GetAssetTotalsByCoinAndAccount(Coin coin, Account account);


        public Task<Result<List<AssetTransaction>>> GetTransactionsByAsset(int assetId);
        //public Task<Result<AssetTransaction>> GetTransactionById(int transactionId);
    }
}

