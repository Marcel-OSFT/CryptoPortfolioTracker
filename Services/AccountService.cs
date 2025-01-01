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

namespace CryptoPortfolioTracker.Services;

public partial class AccountService : ObservableObject, IAccountService
{
    private readonly PortfolioContext context;
    [ObservableProperty] private static ObservableCollection<Account>? listAccounts;
    [ObservableProperty] private static ObservableCollection<AssetAccount>? listAssetAccounts;

    public AccountService(PortfolioContext portfolioContext)
    {
        context = portfolioContext;
    }
    public async Task<ObservableCollection<Account>> PopulateAccountsList()
    {
        var getAccountsResult = await GetAccounts();
        getAccountsResult.IfSucc(s =>
        {
            ListAccounts = new ObservableCollection<Account>(s.OrderByDescending(x => x.TotalValue));
        });

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
        getResult.IfSucc(list => ListAssetAccounts = new(list.OrderByDescending(x => x.Qty))); 
        getResult.IfFail(err =>  ListAssetAccounts = new());

        return ListAssetAccounts;
    }
    public void ClearAccountsByAssetList()
    {
        if (ListAssetAccounts is not null)
        {
            ListAssetAccounts.Clear(); 
        }
    }
    public AssetAccount GetAffectedAccount(Transaction transaction)
    {
        return ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
    }
    public bool IsAccountHoldingAssets(Account account)
    {
        if (account is null || ListAccounts is null) { return false; }
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
        if (ListAccounts is null) { return Task.FromResult(false); }
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
        if (ListAccounts is null || newAccount is null) { return Task.FromResult(false); }
        try
        {
            ListAccounts.Add(newAccount);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }
    public async Task UpdateListAssetAccount(AssetAccount accountAffected)
    {

        if (ListAssetAccounts is null)
        {
            return;
        }
        (await GetAccountByAsset(accountAffected.AssetId))
            .IfSucc(s =>
            {
                var index = -1;

                for (var i = 0; i < ListAssetAccounts.Count; i++)
                {
                    if (ListAssetAccounts[i].Name == accountAffected.Name)
                    {
                        index = i;
                        break;
                    }
                }
                if (index == -1)
                {
                    return;
                }

                if (s != null && s.Name != string.Empty)
                {
                    ListAssetAccounts[index] = s;
                }
                else
                {
                    ListAssetAccounts.RemoveAt(index);
                }
            });
    }
    public async Task<Result<AssetAccount>> GetAccountByAsset(int assetId)
    {
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
        Account account;
        try
        {
            account = context.Accounts.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
        }
        catch (Exception)
        {
            account = new Account();
        }
        return account != null;
    }
}

