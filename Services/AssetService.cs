//using CoinGecko.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Services
{
    public class AssetService : IAssetService
    {
        private readonly PortfolioContext context;
        public static AssetService Instance;


        public AssetService(PortfolioContext portfolioContext)
        {
            Instance = this;
            context = portfolioContext;
        }

        public async Task<Result<List<AssetTotals>>> GetAssetTotals()
        {
            List<AssetTotals> assetsTotals;
            try
            {
                assetsTotals = await context.Assets
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => new AssetTotals
                {
                    Qty = assetGroup.Sum(x => x.Qty),
                    CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                    Coin = assetGroup.Key,
                })
                .ToListAsync();

            }
            catch (Exception ex)
            {
                return new Result<List<AssetTotals>>(ex);
            }
            if (assetsTotals == null) { assetsTotals = new List<AssetTotals>(); }

            return assetsTotals;
        }

        public async Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin)
        {
            if (coin == null) { return new AssetTotals(); }
            AssetTotals assetTotals;
            try
            {
                assetTotals = await context.Assets
               .Where(c => c.Coin.Id == coin.Id)
               .Include(x => x.Coin)
               .GroupBy(asset => asset.Coin)
               .Select(assetGroup => new AssetTotals
               {
                   Qty = assetGroup.Sum(x => x.Qty),
                   CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                   Coin = assetGroup.Key
               })
               .SingleAsync();
            }
            catch (Exception ex)
            {
                return new Result<AssetTotals>(ex);
            }
            return assetTotals;
        }
        public async Task<Result<AssetTotals>> GetAssetTotalsByCoinAndAccount(Coin coin, Account account)
        {
            if (coin == null || account == null) { return new AssetTotals(); }
            AssetTotals assetTotals;
            try
            {
                assetTotals = await context.Assets
               .Include(x => x.Coin)
               .Include(y => y.Account)
               .Where(c => c.Coin.Id == coin.Id && c.Account.Id == account.Id)
               .GroupBy(asset => asset.Coin)
               .Select(assetGroup => new AssetTotals
               {
                   Qty = assetGroup.Sum(x => x.Qty),
                   CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
                   Coin = assetGroup.Key
               })
               .SingleAsync();
            }
            catch (Exception ex)
            {
                return new Result<AssetTotals>(ex);
            }
            return assetTotals;
        }

        public async Task<Result<List<AssetAccount>>> GetAccountsByAsset(int coinId)
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
            if (assetAccounts == null) { assetAccounts = new List<AssetAccount>(); }

            return assetAccounts;
        }

        public async Task<Result<AssetAccount>> GetAccountByAsset(int assetId)
        {
            if (assetId <= 0) { return new AssetAccount(); }
            AssetAccount assetAccount;
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
            // if (assetAccount == null) { assetAccount = new AssetAccount(); }

            return assetAccount;
        }



        public async Task<Result<List<Transaction>>> GetTransactionsByAsset(int assetId)
        {

            if (assetId <= 0) { return new List<Transaction>(); }
            List<Transaction> assetTransactions = null;
            try
            {
                assetTransactions = await context.Mutations
                    .Include(t => t.Transaction)
                    .ThenInclude(m => m.Mutations)
                    .ThenInclude(a => a.Asset)
                    .ThenInclude(ac => ac.Account)
                    .Where(c => c.Asset.Id == assetId)
                    .GroupBy(g => g.Transaction.Id)
                    .Select(grouped => new Transaction
                    {
                        Id = grouped.Key,
                        RequestedAsset = grouped.Select(a => a.Asset).Where(w => w.Id == assetId).SingleOrDefault(),
                        TimeStamp = grouped.Select(t => t.Transaction.TimeStamp).SingleOrDefault(),
                        Note = grouped.Select(t => t.Transaction.Note).SingleOrDefault(),
                        Mutations = grouped.Select(t => t.Transaction.Mutations).SingleOrDefault(),
                    })
                    .OrderByDescending(o => o.TimeStamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return new Result<List<Transaction>>(ex);
            }
            if (assetTransactions == null) { assetTransactions = new List<Transaction>(); }

            return assetTransactions;
        }

    }
}