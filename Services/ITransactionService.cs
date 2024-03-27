using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services
{
    public interface ITransactionService
    {
        public Task<Result<List<string>>> GetCoinSymbolsFromLibrary();
        public Task<Result<Coin>> GetCoinBySymbol(string symbol);
        public Task<Result<List<string>>> GetCoinSymbolsFromLibraryExcluding(string coinSymbol);
        public Task<Result<List<string>>> GetCoinSymbolsFromAssets();
        public Task<Result<List<string>>> GetFeeCoinSymbols(string accountName);
        public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibrary();
       // public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromAssets();
        public Task<Result<List<string>>> GetAccountNames();
        public Task<Result<Account>> GetAccountByName(string name);
        public Task<Result<int>> GetAssetIdByCoinAndAccount(Coin coin, Account account);

        public Task<Result<List<string>>> GetAccountNames(string coinSymbol);
        public Task<Result<List<string>>> GetAccountNamesExcluding(string excludedAccountName);
        public Task<Result<List<string>>> GetUsdtUsdcSymbolsFromAssets();
        public Task<Result<List<string>>> GetUsdtUsdcSymbolsFromLibrary();
        public Task<Result<double[]>> GetMaxQtyAndPrice(string coinSymbol, string accountName);
        public Task<Result<double>> GetPriceFromLibrary(string coinSymbol);
        public Task<Result<bool>> AddTransaction(AssetTransaction transaction);
        public Task<Result<bool>> DeleteTransaction(AssetTransaction transactionToDelete, AssetAccount assetAccountAffected);
        public Task<Result<bool>> EditTransaction(AssetTransaction transactionNew, AssetTransaction transactionOld);
        //public Task<Result<bool>> RemoveAssetsWithoutMutations();
        // public Task<Result<bool>> CoinAndAccountExists(Mutation mutation);
        // public Task<Result<bool>> AssetExists(Mutation mutation);
        // public Task<Result<bool>> AccountExists(Mutation mutation);
        // public Task<Result<bool>> UsdtUsdcAssetExists(Mutation mutation);
        // public Task<Result<bool>> UsdtUsdcCoinAndAccountExists(Mutation mutation);

       // public Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin);
        

    }
}
