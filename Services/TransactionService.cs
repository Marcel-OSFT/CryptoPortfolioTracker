using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Services;

public class TransactionService : ITransactionService
{
    private readonly PortfolioContext _context;
    public static TransactionService Instance;

    public TransactionService(PortfolioContext context)
    {
        Instance = this;
        _context = context;
    }

    public async Task<Result<List<string>>> GetCoinSymbolsFromLibrary()
    {
        List<string> _result;
        try
        {
            _result = await _context.Coins.Select(x => x.Symbol.ToUpper()).ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<Coin>> GetCoinBySymbol(string symbol)
    {
        Coin coin;
        try
        {
            coin = await _context.Coins.Where(x => x.Symbol.ToLower() == symbol.ToLower()).SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<Coin>(ex);
        }
        return coin;
    }
    public async Task<Result<Account>> GetAccountByName(string name)
    {
        Account account;
        try
        {
            account = await _context.Accounts.Where(x => x.Name.ToLower() == name.ToLower()).SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<Account>(ex);
        }
        return account;
    }
    public async Task<Result<List<string>>> GetCoinSymbolsFromLibraryExcluding(string coinSymbol)
    {
        List<string> _result;
        if (coinSymbol == null || coinSymbol == "") { return new List<string>(); }

        try
        {
            _result = await _context.Coins
                .Where(x => x.Symbol.ToLower() != coinSymbol.ToLower())
                .Select(x => x.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(string coinSymbol)
    {
        List<string> _result;
        if (coinSymbol == null || coinSymbol == "") { return new List<string>(); }

        try
        {
            _result = await _context.Coins
                .Where(x => x.Symbol.ToLower() != coinSymbol.ToLower() && x.Symbol.ToLower() != "usdt" && x.Symbol.ToLower() != "usdc")
                .Select(x => x.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }

    public async Task<Result<int>> GetAssetIdByCoinAndAccount(Coin coin, Account account)
    {
        Asset asset;
        try
        {
            asset = await _context.Assets
                .Include(x => x.Coin)
                .Include(y => y.Account)
                .Where(x => x.Coin == coin && x.Account == account)
                .SingleOrDefaultAsync();

        }
        catch (Exception ex)
        {
            return new Result<int>(ex);
        }
        return asset != null ? asset.Id : 0;
    }

    public async Task<Result<List<string>>> GetCoinSymbolsFromAssets()
    {
        List<string> _result;
        try
        {
            _result = await _context.Assets
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => assetGroup.Key.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromAssets()
    {
        List<string> _result;
        try
        {
            _result = await _context.Assets
                .Include(x => x.Coin)
                .Where(s => s.Coin.Symbol.ToLower() != "usdt" && s.Coin.Symbol.ToLower() != "usdc")
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => assetGroup.Key.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetFeeCoinSymbols(string accountName)
    {
        List<string> _result;
        try
        {
            _result = await _context.Assets
                .Include(x => x.Coin)
                .Include(y => y.Account)
                .Where(x => x.Account.Name == accountName)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => assetGroup.Key.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibrary()
    {
        List<string> _result;
        try
        {
            _result = await _context.Coins
                .Where(x => x.Symbol.ToLower() != "usdt" && x.Symbol.ToLower() != "usdc")
                .Select(x => x.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    //public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromAssets()
    //{
    //    List<string> _result;
    //    try
    //    {
    //        _result = await _context.Assets
    //            .Where(x => x.Coin.Symbol.ToLower() != "usdt" && x.Coin.Symbol.ToLower() != "usdc")
    //            .GroupBy(asset => asset.Coin)
    //            .Select(assetGroup => assetGroup.Key.Symbol.ToUpper())
    //            .ToListAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<List<string>>(ex);
    //    }
    //    return _result != null ? _result : new List<string>();
    //}
    public async Task<Result<List<string>>> GetUsdtUsdcSymbolsFromAssets()
    {
        List<string> _result;
        try
        {
            _result = await _context.Assets
                .GroupBy(asset => asset.Coin)
                .Select(assetSymbol => assetSymbol.Key.Symbol.ToUpper())
                .Where(assetSymbol => assetSymbol.ToLower() == "usdt" || assetSymbol.ToLower() == "usdc")
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetUsdtUsdcSymbolsFromLibrary()
    {
        List<string> _result;
        try
        {
            _result = await _context.Coins
                .Where(x => x.Symbol.ToLower() == "usdt" || x.Symbol.ToLower() == "usdc")
                .Select(x => x.Symbol.ToUpper())
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<double[]>> GetMaxQtyAndPrice(string coinSymbol, string accountName)
    {
        double[] _result;
        if (coinSymbol == null || coinSymbol == "" || accountName == null || accountName == "") { return new double[] { 0, 0 }; }

        try
        {
            _result = await _context.Assets
                .Include(x => x.Coin)
                .Where(x => x.Coin.Symbol.ToLower() == coinSymbol.ToLower() && x.Account.Name.ToLower() == accountName.ToLower())
                .Select(i => new double[] { i.Qty, i.Coin.Price })
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<double[]>(ex);
        }
        return _result != null ? _result : new double[] { 0, 0 };
    }
    public async Task<Result<double>> GetPriceFromLibrary(string coinSymbol)
    {
        double _result;
        if (coinSymbol == null || coinSymbol == "") { return 0; }
        try
        {
            _result = await _context.Coins
                .Where(x => x.Symbol.ToLower() == coinSymbol.ToLower())
                .Select(i => i.Price)
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<double>(ex);
        }
        return _result;
    }
    public async Task<Result<List<string>>> GetAccountNames()
    {
        List<string> _result;
        try
        {
            _result = await _context.Accounts.Select(x => x.Name).ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetAccountNames(string coinSymbol)
    {
        List<string> _result;
        if (coinSymbol == null || coinSymbol == "") return new List<string>();
        try
        {
            _result = await _context.Assets
                .Where(x => x.Coin.Symbol.ToLower() == coinSymbol.ToLower())
                .Select(x => x.Account.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    public async Task<Result<List<string>>> GetAccountNamesExcluding(string excludedAccountName)
    {
        List<string> _result;
        if (excludedAccountName == null || excludedAccountName == "") return new List<string>();
        try
        {
            _result = await _context.Accounts
                .Where(x => x.Name.ToLower() != excludedAccountName.ToLower())
                .Select(x => x.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result != null ? _result : new List<string>();
    }
    //public async Task<Result<bool>> CoinAndAccountExists(Mutation mutation)
    //{
    //    if (mutation == null) return false;
    //    Coin coin;
    //    Account account;
    //    try
    //    {
    //        coin = await _context.Coins.Where(x => x.Symbol.ToLower() == mutation.Asset.Coin.Symbol.ToLower()).SingleOrDefaultAsync();
    //        account = await _context.Accounts.Where(x => x.Name.ToLower() == mutation.Asset.Account.Name.ToLower()).SingleOrDefaultAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<bool>(ex);
    //    }
    //    return (coin != null && account != null);
    //}
    //public async Task<Result<bool>> AccountExists(Mutation mutation)
    //{
    //    if (mutation == null) return false;
    //    Account account;
    //    try
    //    {
    //        account = await _context.Accounts.Where(x => x.Name.ToLower() == mutation.Asset.Account.Name.ToLower()).SingleOrDefaultAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<bool>(ex);
    //    }
    //    return (account != null);
    //}
    //public async Task<Result<bool>> AssetExists(Mutation mutation)
    //{
    //    if (mutation == null) return false;
    //    Asset asset;
    //    try
    //    {
    //        asset = await _context.Assets
    //            .Where(x => x.Coin.Symbol.ToLower() == mutation.Asset.Coin.Symbol.ToLower())
    //            .Where(x => x.Account.Name.ToLower() == mutation.Asset.Account.Name.ToLower())
    //            .Where(x => x.Qty >= mutation.Qty)
    //            .SingleOrDefaultAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<bool>(ex);
    //    }
    //    return (asset != null);
    //}
    //public async Task<Result<bool>> UsdtUsdcAssetExists(Mutation mutation)
    //{
    //    if (mutation == null) return false;
    //    Asset asset;
    //    try
    //    {
    //        asset = await _context.Assets
    //            .Where(a => (a.Coin.Symbol.ToLower() == mutation.Asset.Coin.Symbol.ToLower()) && (a.Coin.Symbol.ToLower() == "usdt" || a.Coin.Symbol.ToLower() == "usdc"))
    //            .Where(a => a.Account.Name.ToLower() == mutation.Asset.Account.Name)
    //            .Where(a => a.Qty >= mutation.Qty)
    //            .FirstOrDefaultAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<bool>(ex);
    //    }
    //    return (asset != null);
    //}
    //public async Task<Result<bool>> UsdtUsdcCoinAndAccountExists(Mutation mutation)
    //{
    //    if (mutation == null) return false;
    //    Coin coin;
    //    Account account;
    //    try
    //    {
    //        coin = await _context.Coins
    //            .Where(x => (x.Symbol.ToLower() == mutation.Asset.Coin.Symbol.ToLower()) && (x.Symbol.ToLower() == "usdt" || x.Symbol.ToLower() == "usdc"))
    //            .FirstOrDefaultAsync();
    //        account = await _context.Accounts
    //            .Where(x => x.Name.ToLower() == mutation.Asset.Account.Name.ToLower())
    //            .FirstOrDefaultAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<bool>(ex);
    //    }
    //    return (coin != null && account != null);
    //}

    private Result<Asset> RecalculateAsset(Mutation mutation, Asset asset)
    {
        try
        {
            switch (mutation.Direction.ToString())
            {
                case "In":
                    if ((asset.Qty + mutation.Qty) != 0) //** prevent divide by zero
                    {
                        asset.AverageCostPrice = ((asset.Qty * asset.AverageCostPrice) + (mutation.Qty * mutation.Price)) / (asset.Qty + mutation.Qty);
                        asset.Qty += mutation.Qty;
                    }
                    else
                    {
                        asset.AverageCostPrice = 0;
                        asset.Qty = 0;
                    }
                    break;
                case "Out":
                    //OUT flow does not affect the averageCostPrice of the remaining qty. So no calculation needed for this.
                    asset.Qty -= mutation.Qty;
                    break;
            }
        }
        catch (Exception ex)
        {
            return new Result<Asset>(ex);
        }
        return asset;
    }
    private async Task<Result<Asset>> ReverseAndRecalculateAsset(Mutation mutationNew, Mutation mutationToEdit)
    {
        Asset asset = null;
        try
        {
            asset = await _context.Assets
                .Where(x => x.Id == mutationToEdit.Asset.Id)
                .SingleAsync();

            //*** the only difference between the old and new mutation can be the 'qty' and/or 'price'
            //*** in case of a difference both old qty and price will be reverted,
            //*** followed by applying the new qty and price
            switch (mutationNew.Direction.ToString())
            {
                case "In":
                    //*** revert old
                    if (asset.Qty - mutationToEdit.Qty != 0)
                    {
                        asset.AverageCostPrice = ((asset.Qty * asset.AverageCostPrice) - (mutationToEdit.Qty * mutationToEdit.Price)) / (asset.Qty - mutationToEdit.Qty);
                        asset.Qty -= mutationToEdit.Qty;
                    }
                    else
                    {
                        asset.AverageCostPrice = 0;
                        asset.Qty = 0;
                    }
                    //*** apply new
                    if (asset.Qty + mutationNew.Qty != 0)
                    {
                        asset.AverageCostPrice = ((asset.Qty * asset.AverageCostPrice) + (mutationNew.Qty * mutationNew.Price)) / (asset.Qty + mutationNew.Qty);
                        asset.Qty += mutationNew.Qty;
                    }
                    else
                    {
                        asset.AverageCostPrice = 0;
                        asset.Qty = 0;
                    }
                    break;
                case "Out":
                    //*** revert old
                    //OUT flow does not affect the averageCostPrice of the remaining qty. So no calculation needed for this.
                    asset.Qty += mutationToEdit.Qty;
                    //*** apply new
                    asset.Qty -= mutationNew.Qty;
                    break;
            }

        }
        catch (Exception ex)
        {
            return new Result<Asset>(ex);
        }
        return asset;
    }
    private async Task<Result<Asset>> ReverseAndRecalculateFee(Mutation mutationNew, Mutation mutationToEdit)
    {
        Asset assetOld = null;
        Asset assetNew = null;
        try
        {

            if (mutationToEdit.Asset.Id != mutationNew.Asset.Id || mutationToEdit.Qty != mutationNew.Qty)
            {
                //*** FEE is always "OUT":
                //*** revert old
                assetOld = await _context.Assets
                .Where(x => x.Id == mutationToEdit.Asset.Id)
                .SingleAsync();

                assetOld.Qty += mutationToEdit.Qty;

                //*** apply new
                assetNew = await _context.Assets
                .Where(x => x.Coin.Symbol.ToLower() == mutationNew.Asset.Coin.Symbol.ToLower() && x.Account.Name.ToLower() == mutationNew.Asset.Account.Name.ToLower())
                .SingleAsync();

                assetNew.Qty -= mutationNew.Qty;
            }
        }
        catch (Exception ex)
        {
            return new Result<Asset>(ex);
        }
        return assetNew;
    }
    private Result<Asset> CreateNewAsset(Mutation mutation)
    {
        Asset asset = null;
        if (mutation.Direction == MutationDirection.In) // && currentAsset == NULL
        {
            try
            {
                mutation.Asset.AverageCostPrice = mutation.Price;
                mutation.Asset.Qty = mutation.Qty;
                mutation.Asset.Coin.IsAsset = true;
                //** keep track of newly added asset, because this asset is not yet to be found in the dbcontext
                asset = new Asset();
                asset = mutation.Asset;
            }
            catch (Exception ex)
            {
                return new Result<Asset>(ex);
            }
        }
        return asset;
    }
    private async Task<Result<Asset>> ReverseAndRecalculateAsset(Mutation mutation)
    {
        Asset asset;
        try
        {
            asset = await _context.Assets.Where(x => x.Id == mutation.Asset.Id).SingleAsync();
            switch (mutation.Direction.ToString())
            {
                case "In": //*** revert IN, so subtract instead of add
                    if (asset.Qty - mutation.Qty != 0)
                    {
                        asset.AverageCostPrice = ((asset.Qty * asset.AverageCostPrice) - (mutation.Qty * mutation.Price)) / (asset.Qty - mutation.Qty);
                        asset.Qty -= mutation.Qty;
                    }
                    else
                    {
                        asset.AverageCostPrice = 0;
                        asset.Qty = 0;
                    }
                    break;
                case "Out": //*** Revert OUT, so add instead of subtract
                    //OUT flow does not affect the averageCostPrice of the remaining qty. So no calculation needed for this.
                    asset.Qty += mutation.Qty;
                    break;
            }
        }
        catch (Exception ex)
        {
            return new Result<Asset>(ex);
        }
        return asset;
    }
    //public async Task<bool> EditTransactionNew(Transaction transactionNew, Transaction transactionToEdit)
    //{
    //    bool isDeleted = false;
    //    bool isAdded = false; 

    //    isDeleted =  await DeleteTransaction(transactionToEdit.Id);
    //    if (isDeleted) isAdded = await AddTransaction(transactionNew);

    //    return isAdded;
    //}
    public async Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin)
    {
        if (coin == null) { return new AssetTotals(); }
        AssetTotals assetTotals;
        try
        {
            assetTotals = await _context.Assets
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


    public async Task<Result<int>> AddTransaction(Transaction transaction)
    {
        var result = 0;
        Asset? addedAsset = null;
        if (transaction == null || transaction.Mutations == null) return 0;

        var mutations = transaction.Mutations.OrderByDescending(x => x.Direction).OrderBy(y => y.Type);

        try
        {
            foreach (var mutation in mutations)
            {
                //Check if ASSET (=combination CoinId and AccountId) already exists.
                Asset? currentAsset = await _context.Assets
                    .Where(x => x.Coin.Symbol.ToLower() == mutation.Asset.Coin.Symbol.ToLower() && x.Account.Name.ToLower() == mutation.Asset.Account.Name.ToLower())
                    .SingleOrDefaultAsync();

                if (currentAsset == null && addedAsset != null)
                {
                    currentAsset = addedAsset;
                    addedAsset = null;
                }

                if (currentAsset != null)
                {
                    mutation.Asset = RecalculateAsset(mutation, currentAsset)
                       .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
                }
                else
                {
                    mutation.Asset = CreateNewAsset(mutation)
                       .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
                    // New Asset, so add this to the context
                    addedAsset = mutation.Asset;
                    _context.Assets.Add(mutation.Asset);
                }
            } //End of All mutations

            //******** Transaction transactionNew = new Transaction();
            Transaction transactionNew = new Transaction();
            transactionNew.Mutations = transaction.Mutations;
            transactionNew.TimeStamp = transaction.TimeStamp;
            transactionNew.Note = transaction.Note;

            _context.Transactions.Update(transactionNew);
            result = await _context.SaveChangesAsync();

            return transactionNew.Id;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<int>(ex);
        }
    }

    /// <summary>
    /// Only used for the Run Tests
    /// </summary>
    /// <param name="coinName"></param>
    /// <param name="accountName"></param>
    /// <returns></returns>
    public async Task<Result<Transaction>> GetTransactionById(int transactionId)
    {
        Transaction assetTransaction = null;
        if (transactionId <= 0) return new Result<Transaction>();
        try
        {
            assetTransaction = await _context.Mutations
                .Include(t => t.Transaction)
                .ThenInclude(m => m.Mutations)
                .ThenInclude(a => a.Asset)
                .ThenInclude(ac => ac.Account)
                .Where(c => c.Transaction.Id == transactionId)
                .GroupBy(g => g.Transaction.Id)
                .Select(grouped => new Transaction
                {
                    Id = grouped.Key,
                    TimeStamp = grouped.Select(t => t.Transaction.TimeStamp).SingleOrDefault(),
                    Note = grouped.Select(t => t.Transaction.Note).SingleOrDefault(),
                    Mutations = grouped.Select(t => t.Transaction.Mutations).SingleOrDefault(),
                })
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<Transaction>(ex);
        }
        return assetTransaction;
    }


    public async Task<Result<int>> EditTransaction(Transaction transactionNew, Transaction _transactionToEdit)
    {
        var result = 0;
        if (_transactionToEdit == null || transactionNew == null) return 0;
        try
        {
            var transactionToEdit = await _context.Transactions
                .Where(x => x.Id == _transactionToEdit.Id)
                .Include(x => x.Mutations)
                .ThenInclude(a => a.Asset.Coin)
                .SingleAsync();

            var mutationsNew = new List<Mutation>(transactionNew.Mutations.OrderBy(x => x.Type));
            var mutationsToEdit = new List<Mutation>(transactionToEdit.Mutations.OrderBy(x => x.Type));

            //*** In case of an edit related to a FEE (added or deleted),
            //*** the count of both mutations will NOT be equal which will give an issue when going through
            //*** the mutations side-by-side.
            //*** Adding a dummy FEE mutation with qty 0 will equalize this

            if (mutationsNew.Count != mutationsToEdit.Count)
            {
                EqualizeMutationsForFee(mutationsNew, mutationsToEdit);
            }

            var numberOfMutations = mutationsNew.Count;

            //Go through mutations side by side
            for (var i = 0; i < numberOfMutations; i++)
            {
                Mutation mutationNew = mutationsNew[i];
                Mutation mutationToEdit = mutationsToEdit[i];

                if (mutationToEdit.Type != TransactionKind.Fee)
                {
                    mutationToEdit.Asset = (await ReverseAndRecalculateAsset(mutationNew, mutationToEdit))
                        .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
                    mutationToEdit.Qty = mutationNew.Qty;
                    mutationToEdit.Price = mutationNew.Price;
                    //_context.Mutations.Update(mutationToEdit);
                }
                else // if 'Fee'
                {
                    mutationToEdit.Asset = (await ReverseAndRecalculateFee(mutationNew, mutationToEdit))
                        .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
                    mutationToEdit.Qty = mutationNew.Qty;
                    //_context.Mutations.Update(mutationToEdit);
                }
            } // *** end of all mutations

            //*** update Notes and TimeStamp in case that might have changed
            transactionToEdit.Note = transactionNew.Note;
            transactionToEdit.TimeStamp = transactionNew.TimeStamp;
            transactionToEdit.Mutations = mutationsToEdit;

            _context.Transactions.Update(transactionToEdit);
            result = await _context.SaveChangesAsync();

            //*** reflect type 'Transaction' also to 'Transaction' for Binding purpose
            _transactionToEdit.Mutations = transactionToEdit.Mutations;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<int>(ex);
        }
        return _transactionToEdit.Id;
    }
    public async Task<Result<int>> DeleteTransaction(Transaction _transactionToDelete, AssetAccount _accountAffected)
    {
        var result = 0;

        try
        {
            Transaction transaction = await _context.Transactions
                .Where(x => x.Id == _transactionToDelete.Id)
                .Include(m => m.Mutations)
                .ThenInclude(m => m.Asset)
                .SingleAsync();

            foreach (var mutation in transaction.Mutations.OrderBy(x => x.Type))
            {
                mutation.Asset = (await ReverseAndRecalculateAsset(mutation))
                    .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err)); ;
                _context.Mutations.Remove(mutation);
            }
            _context.Transactions.Remove(transaction);
            result = _context.SaveChanges();
            await RemoveAssetsWithoutMutations();
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<int>(ex);
        }
        return _transactionToDelete.Id;
    }
    public async Task<Result<bool>> RemoveAssetsWithoutMutations()
    {
        var result = 0;
        //*** Due to deletion of a transaction it could be that an asset (coin/account combi) doesn't have any mutations left.
        //*** In that case the qty for this coin in that specific account will also be zero and the asset can be removed as well
        try
        {
            var assetsWithoutMutations = await _context.Assets.Where(x => x.Mutations.Count == 0).ToListAsync();
            if (assetsWithoutMutations != null)
            {
                foreach (var asset in assetsWithoutMutations)
                {
                    _context.Assets.Remove(asset);
                }
                result = _context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        return result > 0;
    }
    private void EqualizeMutationsForFee(List<Mutation> mutsNew, List<Mutation> mutsToEdit)
    {
        Mutation dummyMutation;

        if (mutsNew.Count > mutsToEdit.Count)
        {
            dummyMutation = (Mutation)CloneMutation(mutsNew.Last());
            dummyMutation.Qty = 0;
            mutsToEdit.Add(dummyMutation);

        }
        else
        {
            dummyMutation = (Mutation)CloneMutation(mutsToEdit.Last());
            dummyMutation.Qty = 0;
            mutsNew.Add(dummyMutation);
        }
    }
    private object CloneMutation(Mutation mut)
    {
        var cloneMut = new Mutation();

        var properties = mut.GetType().GetProperties();
        foreach (var property in properties)
        {
            var value = property.GetValue(mut);
            if (cloneMut.GetType().GetProperty(property.Name) is PropertyInfo prop)
            {
                prop.SetValue(cloneMut, value);
            }
        }
        return cloneMut;
    }
    private void RejectChanges()
    {
        foreach (var entry in _context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.State = EntityState.Modified; //Revert changes made to deleted entity.
                    entry.State = EntityState.Unchanged;
                    break;
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
            }
        }
    }

}
