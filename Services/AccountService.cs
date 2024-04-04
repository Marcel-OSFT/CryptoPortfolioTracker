//using CoinGecko.Clients;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Services
{
    public class AccountService : IAccountService
    {
        private readonly PortfolioContext context;
        public AccountService(PortfolioContext portfolioContext)
        {
            context = portfolioContext;
        }

        public async Task<Result<bool>> CreateAccount(Account newAccount)
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
        //public async Task<bool> EditAccount(Account newAccount, Account accountToEdit)
        //{
        //    bool _result;
        //    if (newAccount == null || newAccount.Name == "" || accountToEdit == null) { return false; }
        //    try
        //    {
        //        var account = await context.Accounts
        //            .Where(x => x.Id == accountToEdit.Id)
        //            .SingleAsync();

        //        account.Name = newAccount.Name;
        //        account.About = newAccount.About;
        //        context.Accounts.Update(account);
        //        _result = await context.SaveChangesAsync() > 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //    return _result;
        //}

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

        public async Task<Result<List<Account>>> GetAccounts()
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
                    account.IsHoldingAsset = account.Assets != null && account.Assets.Count > 0 ? true : false;
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
                    account.IsHoldingAsset = account.Assets != null && account.Assets.Count > 0 ? true : false;
            }
            catch (Exception ex)
            {
                return new Result<Account>(ex);
            }
            return account;
        }

        //public async Task<Result<List<Account>>> GetAccountsOrderByName()
        //{
        //    List<Account> accountList;
        //    try
        //    {
        //        accountList = await context.Accounts.OrderBy(x => x.Name).ToListAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        return new Result<List<Account>>(ex);
        //    }
        //    return accountList != null ? accountList : new List<Account>();
        //}

        public async Task<Result<List<AssetTotals>>> GetAssetsByAccount(int accountId)
        {
            List<AssetTotals> assetsTotals;
            if (accountId <= 0) { return new List<AssetTotals>(); }
            try
            {
                assetsTotals = await context.Assets
                .Where(c => c.Account.Id == accountId)
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => new AssetTotals
                {
                    Qty = assetGroup.Sum(x => x.Qty),
                    CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                    Coin = assetGroup.Key
                })
                .ToListAsync();
            }
            catch (Exception ex)
            {
                return new Result<List<AssetTotals>>(ex);
            }
            return assetsTotals;
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
            return account.Assets == null || account.Assets.Count == 0 ? true : false;
        }

    }
}

