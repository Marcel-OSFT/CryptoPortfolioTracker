using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Helpers;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using LanguageExt.ClassInstances;

namespace CryptoPortfolioTracker.Services;

public partial class AccountService : ObservableObject, IAccountService
{
    private readonly PortfolioService _portfolioService;
    [ObservableProperty] private static ObservableCollection<Account>? listAccounts;
    [ObservableProperty] private static ObservableCollection<AssetAccount>? listAssetAccounts;

    public AccountService(PortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    public async Task<ObservableCollection<Account>> PopulateAccountsList()
    {
        var getAccountsResult = await GetAccounts();
        ListAccounts = getAccountsResult.Match(
            list => new ObservableCollection<Account>(list.OrderByDescending(x => x.TotalValue)),
            err => new ObservableCollection<Account>() 
        );
        return ListAccounts;
    }

    public void ReloadValues()
    {
        var tempListAccounts = ListAccounts;
        ListAccounts = null;
        ListAccounts = tempListAccounts;
    }


    public async Task<ObservableCollection<AssetAccount>> PopulateAccountsByAssetList(int coinId)
    {
        var getResult = await GetAccountsByAsset(coinId);
        ListAssetAccounts = getResult.Match(
            list => new ObservableCollection<AssetAccount>(list.OrderByDescending(x => x.Qty)),
            err => new ObservableCollection<AssetAccount>()
        );

        return ListAssetAccounts;
    }


    public void ClearAccountsByAssetList()
    {
        if (ListAssetAccounts != null && ListAssetAccounts.Any())
        {
            ListAssetAccounts.Clear(); 
        }
    }
    public AssetAccount GetAffectedAccount(Transaction transaction)
    {
        if (ListAssetAccounts == null && transaction.RequestedAsset == null)
        {
            throw new InvalidOperationException("ListAssetAccounts or RequestedAsset is null");
        }

        return ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
    }
    public bool IsAccountHoldingAssets(Account account)
    {
        if (account is null || ListAccounts is null || !ListAccounts.Any()) { return false; }
        var result = false;
        try
        {
            result = ListAccounts.Where(x => x.Id == account.Id).Single().IsHoldingAsset;
        }
        catch (Exception)
        {
            //Element just removed from the list...
        }
        return result;
    }
    public Task RemoveFromListAccounts(int accountId)
    {
        if (ListAccounts is null || !ListAccounts.Any()) { return Task.FromResult(false); }
        try
        {
            var account = ListAccounts.Where(x => x.Id == accountId).Single();
            ListAccounts.Remove(account);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
    public Task AddToListAccounts(Account? newAccount)
    {
        if (ListAccounts is null || !ListAccounts.Any() || newAccount is null) { return Task.FromResult(false); }
        try
        {
            ListAccounts.Add(newAccount);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }
    public async Task UpdateListAssetAccount(AssetAccount accountAffected)
    {
        if (ListAssetAccounts is null || !ListAssetAccounts.Any()) return;

        var result = await GetAccountByAsset(accountAffected.AssetId);
        if (result.IsFaulted) return;

        var updatedAccount = result.IfFail(new AssetAccount());
        if (updatedAccount == null || string.IsNullOrEmpty(updatedAccount.Name)) return;

        var index = ListAssetAccounts.IndexOf(ListAssetAccounts.FirstOrDefault(a => a.Name == accountAffected.Name));
        if (index == -1) return;

        ListAssetAccounts[index] = updatedAccount;
    }
    public async Task<Result<AssetAccount>> GetAccountByAsset(int assetId)
    {
        var context = _portfolioService.Context;
        if (assetId <= 0) { return new AssetAccount(); }
        AssetAccount? assetAccount;
        try
        {
            assetAccount = await context.Assets
                .Where(c => c.Id == assetId)
                .Include(x => x.Coin)
                .Include(a => a.Account)
                .Select(assets => new AssetAccount
                {
                    Qty = assets.Qty,
                    Name = assets.Account.Name,
                    Symbol = assets.Coin.Symbol,
                    AssetId = assets.Id,

                }).SingleOrDefaultAsync();
        }
        catch (Exception ex)
        {
            return new Result<AssetAccount>(ex);
        }

        return assetAccount ?? new AssetAccount();
    }
    public async Task<Result<bool>> CreateAccount(Account? newAccount)
    {
        var context = _portfolioService.Context;
        bool _result;

        if (newAccount == null) { return false; }
        try
        {
            context.Accounts.Add(newAccount);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        return _result;
    }
    public async Task<Result<bool>> EditAccount(Account newAccount, Account accountToEdit)
    {
        var context = _portfolioService.Context;
        bool _result;
        if (newAccount == null || newAccount.Name == "" || accountToEdit == null) { return false; }
        try
        {
            var account = await context.Accounts
                .Where(x => x.Id == accountToEdit.Id)
                .SingleAsync();

            account.Name = newAccount.Name;
            account.About = newAccount.About;
            context.Accounts.Update(account);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        return _result;
    }
    public async Task<Result<bool>> RemoveAccount(int accountId)
    {
        var context = _portfolioService.Context;
        bool _result;
        if (accountId <= 0) { return false; }
        try
        {
            var account = await context.Accounts.Where(x => x.Id == accountId).SingleAsync();
            context.Accounts.Remove(account);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        return _result;
    }
    private async Task<Result<List<Account>>> GetAccounts()
    {
        var context = _portfolioService.Context;
        List<Account> accounts;
        try
        {
            accounts = await context.Accounts
                .Include(x => x.Assets)
                .ThenInclude(c => c.Coin)
                .ToListAsync();

            if (accounts == null || accounts.Count == 0) { return new List<Account>(); }

            foreach (var account in accounts)
            {
                account.CalculateTotalValue();
                account.IsHoldingAsset = account.Assets != null && account.Assets.Count > 0;
            }
        }
        catch (Exception ex)
        {
            return new Result<List<Account>>(ex);
        }
        return accounts;
    }
    public async Task<Result<Account>> GetAccountByName(string name)
    {
        var context = _portfolioService.Context;
        Account account;
        try
        {
            account = await context.Accounts
                .Include(x => x.Assets)
                .ThenInclude(c => c.Coin)
                .Where(x => x.Name.ToLower() == name.ToLower())
                .SingleAsync();

            account.CalculateTotalValue();
            account.IsHoldingAsset = account.Assets != null && account.Assets.Count > 0;
        }
        catch (Exception ex)
        {
            return new Result<Account>(ex);
        }
        return account;
    }
    public async Task<Result<bool>> AccountHasNoAssets(int accountId)
    {
        var context = _portfolioService.Context;
        Account account;
        if (accountId <= 0) { return false; }
        try
        {
            account = await context.Accounts
            .Where(c => c.Id == accountId)
            .Include(x => x.Assets)
            .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        return account.Assets == null || account.Assets.Count == 0;
    }
    private async Task<Result<List<AssetAccount>>> GetAccountsByAsset(int coinId)
    {
        var context = _portfolioService.Context;
        if (coinId <= 0) { return new List<AssetAccount>(); }
        List<AssetAccount> assetAccounts;
        try
        {
            assetAccounts = await context.Assets
                .Include(x => x.Coin)
                .Where(c => c.Coin.Id == coinId)
                .Include(a => a.Account)
                .Select(assets => new AssetAccount
                {
                    Qty = assets.Qty,
                    Name = assets.Account.Name,
                    Symbol = assets.Coin.Symbol,
                    AssetId = assets.Id,

                }).ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<AssetAccount>>(ex);
        }
        assetAccounts ??= new List<AssetAccount>();

        return assetAccounts;
    }


    public bool DoesAccountNameExist(string name)
    {
        var context = _portfolioService.Context;
        try
        {
            return context.Accounts.Any(x => x.Name.ToLower() == name.ToLower());
        }
        catch (Exception)
        {
            return false;
        }
    }
}

