using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface IAccountService
{
    public Task<Result<bool>> CreateAccount(Account? newAccount);
    public Task<Result<bool>> EditAccount(Account newAccount, Account accountToEdit);
    public Task<Result<bool>> RemoveAccount(int accountId);
    //public Task<Result<List<Account>>> GetAccounts();
    //public Task<Result<Account>> GetAccountByName(string name);
    public Task<Result<bool>> AccountHasNoAssets(int assetId);
    Task<ObservableCollection<Account>> PopulateAccountsList();
    Task<ObservableCollection<AssetAccount>> PopulateAccountsByAssetList(int coinId);
    Task<Result<AssetAccount>> GetAccountByAsset(int assetId);
    Task UpdateListAssetAccount(AssetAccount accountAffected);
    void ClearAccountsByAssetList();
    bool IsAccountHoldingAssets(Account account);
    Task RemoveFromListAccounts(int accountId);
    Task AddToListAccounts(Account? newAccount);
    Task UpdateListAccounts(Account? account);
    AssetAccount GetAffectedAccount(Transaction transaction);
    void ReloadValues();
    bool DoesAccountNameExist(string name);
}
