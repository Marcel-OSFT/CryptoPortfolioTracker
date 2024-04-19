using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services
{
    public interface IAccountService
    {
        public Task<Result<bool>> CreateAccount(Account newAccount);
        public Task<Result<bool>> EditAccount(Account newAccount, Account accountToEdit);
        //public Task<bool> EditAccount(Account newAccount, Account accountToEdit);
        public Task<Result<bool>> RemoveAccount(int accountId);
        public Task<Result<List<Account>>> GetAccounts();
        public Task<Result<Account>> GetAccountByName(string name);
        public Task<Result<List<AssetTotals>>> GetAssetsByAccount(int accountId);
        public Task<Result<bool>> AccountHasNoAssets(int assetId);
    }
}
