﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface ITransactionService
{
    public Task<Result<List<string>>> GetCoinSymbolsFromLibrary();
    public Task<Result<Coin>> GetCoinBySymbol(string symbol);
    public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(string coinSymbol);
    public Task<Result<List<string>>> GetCoinSymbolsFromAssets();
    public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibrary();
    public Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromAssets();
    public Task<Result<List<string>>> GetAccountNames();
    public Task<Result<Account>> GetAccountByName(string name);
    public Task<Result<int>> GetAssetIdByCoinAndAccount(Coin coin, Account account);
    public Task<Result<List<string>>> GetAccountNames(string coinSymbol);
    public Task<Result<List<string>>> GetAccountNamesExcluding(string excludedAccountName);
    public Task<Result<List<string>>> GetUsdtUsdcSymbolsFromAssets();
    public Task<Result<List<string>>> GetUsdtUsdcSymbolsFromLibrary();
    public Task<Result<double[]>> GetMaxQtyAndPrice(string coinSymbol, string accountName);
    public Task<Result<double>> GetPriceFromLibrary(string coinSymbol);
    public Task<Result<int>> AddTransaction(Transaction transaction);
    public Task<Result<int>> DeleteTransaction(Transaction transactionToDelete, AssetAccount assetAccountAffected);
    public Task<Result<int>> EditTransaction(Transaction transactionNew, Transaction transactionOld);
    public Task<Result<Transaction>> GetTransactionById(int transactionId);
}
