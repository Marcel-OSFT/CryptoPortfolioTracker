
using CryptoPortfolioTracker.Infrastructure.Response.Coins;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public interface IAccountService
    {
        public Task<Result<bool>> CreateAccount(Account newAccount);
        public Task<Result<bool>> EditAccount(Account newAccount, Account accountToEdit);
        //public Task<bool> EditAccount(Account newAccount, Account accountToEdit);
        public Task<Result<bool>> RemoveAccount(int accountId);
        public Task<Result<List<Account>>> GetAccounts();
        public Task<Result<List<AssetTotals>>> GetAssetsByAccount(int accountId);
        public Task<Result<bool>> AccountHasNoAssets(int assetId);
    }
}
