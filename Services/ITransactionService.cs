using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface ITransactionService
{
    public Task<Result<List<string>>> GetCoinSymbolsFromLibrary();
    public Task<Result<Coin>> GetCoinBySymbol(string symbolName);
    public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(string symbolName);
    public Task<Result<List<string>>> GetCoinSymbolsFromAssets();
    public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibrary();
    public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromAssets();
    public Task<Result<List<string>>> GetAccountNames();
    public Task<Result<Account>> GetAccountByName(string name);
    public Task<Result<int>> GetAssetIdByCoinAndAccount(Coin coin, Account account);
    public Task<Result<List<string>>> GetAccountNames(string symbolName);
    public Task<Result<List<string>>> GetAccountNamesExcluding(string excludedAccountName);
    public Task<Result<List<string>>> GetUsdtUsdcSymbolsFromAssets();
    public Task<Result<List<string>>> GetUsdtUsdcSymbolsFromLibrary();
    public Task<Result<double[]>> GetMaxQtyAndPrice(string symbolName, string accountName);
    public Task<Result<double>> GetPriceFromLibrary(string symbolName);
    public Task<Result<int>> AddTransaction(Transaction transaction);
    public Task<Result<int>> DeleteTransaction(Transaction transactionToDelete, AssetAccount assetAccountAffected);
    public Task<Result<int>> EditTransaction(Transaction transactionNew, Transaction transactionOld);
    public Task<Result<Transaction>> GetTransactionById(int transactionId);
    Task<ObservableCollection<Transaction>> PopulateTransactionsByAssetList(int assetId);
    Task UpdateListAssetTransactions(Transaction transactionNew, Transaction transactionToEdit);
    Task<bool> RemoveFromListAssetTransactions(Transaction deletedTransaction);
    void ClearAssetTransactionsList();
}
