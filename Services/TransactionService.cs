using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Helpers;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;

namespace CryptoPortfolioTracker.Services;

public partial class TransactionService :  ObservableObject, ITransactionService
{
    private readonly PortfolioService _portfolioService;
    [ObservableProperty] private static ObservableCollection<Transaction>? listAssetTransactions;

    public TransactionService(PortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    public async Task<ObservableCollection<Transaction>> PopulateTransactionsByAssetList(int assetId)
    {
        var getResult = await GetTransactionsByAsset(assetId);
        ListAssetTransactions = getResult.Match(
            list => new ObservableCollection<Transaction>(list),
            err => new());
        
        return ListAssetTransactions;
    }
    public void ClearAssetTransactionsList()
    {
        if (ListAssetTransactions is not null)
        {
            //ListAssetTransactions.Clear();
            ListAssetTransactions = new();
        }
    }
    public Task UpdateListAssetTransactions(Transaction transactionNew, Transaction transactionToEdit)
    {

        if (ListAssetTransactions is null || !ListAssetTransactions.Any()) { return Task.CompletedTask; }

        var index = -1;
        for (var i = 0; i < ListAssetTransactions.Count; i++)
        {
            if (ListAssetTransactions[i].Id == transactionToEdit.Id)
            {
                index = i;
                break;
            }
        }
        if (index >= 0)
        {
            ListAssetTransactions[index] = transactionNew;
        }
        return Task.CompletedTask;
    }
    public Task<bool> RemoveFromListAssetTransactions(Transaction deletedTransaction)
    {
        if (ListAssetTransactions is null || !ListAssetTransactions.Any()) { return Task.FromResult(false); }
        var transactionToUpdate = ListAssetTransactions.Where(x => x.Id == deletedTransaction.Id).Single();
        ListAssetTransactions.Remove(deletedTransaction);
        return Task.FromResult(true);
    }
    public async Task<Result<List<string>>> GetCoinSymbolsFromLibrary()
    {
        var context = _portfolioService.Context;
        List<string> _result;
        try
        {
           _result = await context.Coins
                .AsNoTracking()
                .Select(x => x.Symbol.ToUpper() + " " + x.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }

    public async Task<Result<Coin>> GetCoinBySymbol(string symbolName)
    {
        var context = _portfolioService.Context;
        Coin coin;
        var _symbol = symbolName.Split(' ',2);
        try
        {
            coin = await context.Coins
                .AsNoTracking()
                .Where(x => x.Symbol.ToLower() == _symbol[0].ToLower() 
                    && x.Name.ToLower() == _symbol[1].ToLower() )
                .Include(x => x.Narrative)
                .Include(x => x.PriceLevels)
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<Coin>(ex);
        }
        return coin;
    }
    public async Task<Result<Account>> GetAccountByName(string name)
    {
        var context = _portfolioService.Context;
        Account account;
        try
        {
            account = await context.Accounts
                .AsNoTracking()
                .Where(x => x.Name.ToLower() == name.ToLower())
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<Account>(ex);
        }
        return account;
    }
    public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibraryExcluding(string symbolName)
    {
        var context = _portfolioService.Context;
        List<string> _result;
        var _symbol = symbolName.Split(' ',2);
        if (symbolName == null || symbolName == "") { return new List<string>(); }

        try
        {
            _result = await context.Coins
                .AsNoTracking()
                .Where(x => 
                     !(x.Symbol.ToLower() == _symbol[0].ToLower() 
                     && x.Name.ToLower() == _symbol[1].ToLower())
                    && x.Symbol.ToLower() != "usdt" 
                    && x.Symbol.ToLower() != "usdc")
                .Select(x => x.Symbol.ToUpper() + " " + x.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<int>> GetAssetIdByCoinAndAccount(Coin coin, Account account)
    {
        var context = _portfolioService.Context;
        Asset? asset;
        try
        {
            asset = await context.Assets
                .AsNoTracking()
                .Where(x => x.Coin == coin && x.Account == account)
                .Include(x => x.Coin)
                .Include(y => y.Account)
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
        var context = _portfolioService.Context;
        List<string> _result;
        try
        {
            _result = await context.Assets
                .AsNoTracking()
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => assetGroup.Key.Symbol.ToUpper() + " " + assetGroup.Key.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromAssets()
    {
        var context = _portfolioService.Context;
        List<string> _result;
        try
        {
            _result = await context.Assets
                .AsNoTracking()
                .Where(s => s.Coin.Symbol.ToLower() != "usdt" && s.Coin.Symbol.ToLower() != "usdc")
                .Include(x => x.Coin)
                .GroupBy(asset => asset.Coin)
                .Select(assetGroup => assetGroup.Key.Symbol.ToUpper() + " " + assetGroup.Key.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<List<string>>> GetCoinSymbolsExcludingUsdtUsdcFromLibrary()
    {
        var context = _portfolioService.Context;
        List<string> _result;
        try
        {
            _result = await context.Coins
                .AsNoTracking()
                .Where(x => 
                    x.Symbol.ToLower() != "usdt" 
                    && x.Symbol.ToLower() != "usdc")
                .Select(x => x.Symbol.ToUpper() + " " + x.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<List<string>>> GetUsdtUsdcSymbolsFromAssets()
    {
        var context = _portfolioService.Context;
        List<string> _result;
        try
        {
            _result = await context.Assets
                .AsNoTracking()
               .Where(s => 
                    s.Coin.Symbol.ToLower() == "usdt" 
                    || s.Coin.Symbol.ToLower() == "usdc")
               .Include(x => x.Coin)
               .GroupBy(asset => asset.Coin)
               .Select(assetGroup => assetGroup.Key.Symbol.ToUpper() + " " + assetGroup.Key.Name)
               .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<List<string>>> GetUsdtUsdcSymbolsFromLibrary()
    {
        var context = _portfolioService.Context;
        List<string> _result;
        try
        {
            _result = await context.Coins
                .AsNoTracking()
                .Where(x => 
                    x.Symbol.ToLower() == "usdt" 
                    || x.Symbol.ToLower() == "usdc")
                .Select(x => x.Symbol.ToUpper() + " " + x.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<double[]>> GetMaxQtyAndPrice(string symbolName, string accountName)
    {
        var context = _portfolioService.Context;
        double[] _result;
        var _symbol = symbolName.Split(' ', 2);

        if (symbolName == null || symbolName == "" || accountName == null || accountName == "") { return new double[] { 0, 0, 0 }; }

        try
        {
            _result = await context.Assets
                .AsNoTracking()
                .Where(x =>
                    x.Coin.Symbol.ToLower() == _symbol[0].ToLower()
                    && x.Coin.Name.ToLower() == _symbol[1].ToLower()
                    && x.Account.Name.ToLower() == accountName.ToLower())
                .Include(x => x.Coin)
                .Select(i => new double[] { i.Qty, i.Coin.Price, i.AverageCostPrice })
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<double[]>(ex);
        }
        return _result ?? (new double[] { 0, 0, 0 });
    }
    public async Task<Result<double>> GetPriceFromLibrary(string symbolName)
    {
        var context = _portfolioService.Context;
        double _result;
        var _symbol = symbolName.Split(' ', 2);

        if (symbolName == null || symbolName == "") { return 0; }
        try
        {
            _result = await context.Coins
                .AsNoTracking()
                .Where(x => 
                    x.Symbol.ToLower() == _symbol[0].ToLower()
                    && x.Name.ToLower() == _symbol[1].ToLower())
                .Select(i => i.Price)
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<double>(ex);
        }
        return _result;
    }
    
   
    public async Task<Result<List<string>>> GetAccountNames(string? symbolName)
    {
        var context = _portfolioService.Context;
        List<string> _result;

        try
        {
            if (symbolName == null || symbolName == "")
            {
                _result = await context.Accounts.Select(x => x.Name).ToListAsync();
            }
            else
            {
                var _symbol = symbolName.Split(' ', 2);

                _result = await context.Assets
                    .AsNoTracking()
                    .Where(x =>
                        x.Coin.Symbol.ToLower() == _symbol[0].ToLower()
                        && x.Coin.Name.ToLower() == _symbol[1].ToLower())
                    .Select(x => x.Account.Name)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    public async Task<Result<List<string>>> GetAccountNamesExcluding(string excludedAccountName)
    {
        var context = _portfolioService.Context;
        List<string> _result;
        if (excludedAccountName == null || excludedAccountName == "")
        {
            return new List<string>();
        }

        try
        {
            _result = await context.Accounts
                .AsNoTracking()
                .Where(x => x.Name.ToLower() != excludedAccountName.ToLower())
                .Select(x => x.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<string>>(ex);
        }
        return _result ?? new List<string>();
    }
    //public async Task<Result<AssetTotals>> GetAssetTotalsByCoin(Coin coin)
    //{
    //    var context = _portfolioService.Context;
    //    if (coin == null) { return new AssetTotals(); }
    //    AssetTotals assetTotals;
    //    try
    //    {
    //        assetTotals = await context.Assets
    //            .AsNoTracking()
    //           .Where(c => c.Coin.Id == coin.Id)
    //           .Include(x => x.Coin)
    //           .GroupBy(asset => asset.Coin)
    //           .Select(assetGroup => new AssetTotals
    //           {
    //               Qty = assetGroup.Sum(x => x.Qty),
    //               CostBase = assetGroup.Sum(x => x.AverageCostPrice * x.Qty),
    //               Coin = assetGroup.Key
    //           })
    //           .SingleAsync();
    //    }
    //    catch (Exception ex)
    //    {
    //        return new Result<AssetTotals>(ex);
    //    }
    //    return assetTotals;
    //}
    public async Task<Result<int>> AddTransaction(Transaction transaction)
    {
        if (!IsValidTransaction(transaction))
        {
            return 0;
        }

        var mutations = transaction.Mutations.OrderByDescending(x => x.Direction).OrderBy(y => y.Type);

        try
        {
            var context = _portfolioService.Context;
            context.ChangeTracker?.Clear();
            foreach (var mutation in mutations)
            {
                //Check if ASSET (=combination CoinId and AccountId) already exists.
                //Asset? currentAsset = null;
                Asset? addedAsset = null;
                Asset? currentAsset = null;
                
                CheckForExistingAsset(context, mutation, addedAsset, ref currentAsset);

                if (currentAsset != null)
                {
                    mutation.Asset = RecalculateAsset(mutation, currentAsset)
                       .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
                }
                else
                {
                    mutation.Asset = CreateNewAsset(context, mutation)
                       .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
                    // New Asset, so add this to the context
                    if (mutation.Asset is not null)
                    {
                        context.Assets.Add(mutation.Asset);

                        //add MarketCharts for this new Asset
                        var suffix = mutation.Asset.Coin.Name.Contains("_pre-listing") ? "-prelisting" : "";
                        var fileName = App.ChartsFolder + "\\MarketChart_" + mutation.Asset.Coin.ApiId + suffix + ".json";
                        await using FileStream createStream = File.Create(fileName);
                    }
                    addedAsset = mutation.Asset;
                }
            } //End of All mutations

            //******** Transaction transactionNew = new Transaction();
            var transactionNew = TransactionBuilder.Create()
                .WithMutations(transaction.Mutations)
                .ExecutedOn(transaction.TimeStamp)
                .WithNote(transaction.Note)
                .Build();

            context.Transactions.Add(transactionNew);
           // context.Transactions.Update(transactionNew);
            await context.SaveChangesAsync();
            context.ChangeTracker?.Clear();
            return transactionNew.Id;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<int>(ex);
        }
    }

    private void CheckForExistingAsset(PortfolioContext context, Mutation? mutation, Asset? addedAsset, ref Asset? currentAsset)
    {
        currentAsset = mutation.Asset is not null
                            ? context.Assets
                                //.AsNoTracking()
                                .Where(x => x.Coin.Symbol.ToLower() == mutation.Asset.Coin.Symbol.ToLower() && x.Account.Name.ToLower() == mutation.Asset.Account.Name.ToLower())
                                .Include(x => x.Coin)
                                .Include (x => x.Account)
                                .SingleOrDefault()
                                : null;

        if (currentAsset == null && addedAsset != null)
        {
            currentAsset = addedAsset;
            addedAsset = null;
        }
       
    }

    private static bool IsValidTransaction(Transaction transaction)
    {
        return !(transaction == null || transaction.Mutations == null || transaction.Mutations.Count == 0);
    }

    public async Task<Result<int>> EditTransaction(Transaction transactionNew, Transaction _transactionToEdit)
    {
        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        var result = 0;
        if (_transactionToEdit == null || transactionNew == null)
        {
            return 0;
        }
        try
        {
            var transactionToEdit = await context.Transactions
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
                NormalizeMutationsForFee(mutationsNew, mutationsToEdit);
            }

            var numberOfMutations = mutationsNew.Count;
            //Go through mutations side by side
            for (var i = 0; i < numberOfMutations; i++)
            {
                var mutationNew = mutationsNew[i];
                var mutationToEdit = mutationsToEdit[i];

                if (IsMutationModified(mutationNew, mutationToEdit))
                {
                    await UpdateMutationAndRecalculateAsset(mutationNew, mutationToEdit);
                }
            } // *** end of all mutations

            //*** update Notes and TimeStamp in case that might have changed
            transactionToEdit.Note = transactionNew.Note;
            transactionToEdit.TimeStamp = transactionNew.TimeStamp;
            transactionToEdit.Mutations = mutationsToEdit;

            context.Transactions.Update(transactionToEdit);
            result = await context.SaveChangesAsync();

            //*** reflect type 'Transaction' also to 'Transaction' for Binding purpose
            _transactionToEdit.Mutations = transactionToEdit.Mutations;
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<int>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _transactionToEdit.Id;
    }

    private static bool IsMutationModified(Mutation mutationNew, Mutation mutationToEdit)
    {
        return !(mutationToEdit.Price.Equals(mutationNew.Price) && mutationToEdit.Qty.Equals(mutationNew.Qty));
    }

    public async Task<Result<int>> DeleteTransaction(Transaction _transactionToDelete, AssetAccount _accountAffected)
    {
        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        var result = 0;
        try
        {
            var transaction = await context.Transactions
                .Where(x => x.Id == _transactionToDelete.Id)
                .Include(m => m.Mutations)
                .ThenInclude(m => m.Asset)
                .SingleAsync();

            foreach (var mutation in transaction.Mutations.OrderBy(x => x.Type))
            {
                mutation.Asset = (await ReverseAndRecalculateAsset(mutation))
                    .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err)); ;
                context.Mutations.Remove(mutation);
            }
            context.Transactions.Remove(transaction);
            result = context.SaveChanges();
            context.ChangeTracker?.Clear();
            await RemoveAssetsWithoutMutations();
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<int>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _transactionToDelete.Id;
    }
    
    private async Task<Result<bool>> RemoveAssetsWithoutMutations()
    {
        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        var result = 0;
        //*** Due to deletion of a transaction it could be that an asset (coin/account combi) doesn't have any mutations left.
        //*** In that case the qty for this coin in that specific account will also be zero and the asset can be removed as well
        try
        {
            var assetsWithoutMutations = await context.Assets
                .Where(x => x.Mutations.Count == 0)
                .Include(x => x.Coin)
                .ToListAsync();
            if (assetsWithoutMutations != null)
            {
                foreach (var asset in assetsWithoutMutations)
                {
                    context.Assets.Remove(asset);
                    //*** if this assets coin does not exists in any other Asset having mutations, then set 'IsAsset' to false
                    var assetCoin = await context.Assets
                        .Where(x => x.Coin.Id == asset.Coin.Id && x.Mutations.Count > 0)
                        .ToListAsync();
                    if (assetCoin is null || assetCoin.Count == 0)
                    {
                        var coin = await context.Coins
                            .Where(x => x.Id == asset.Coin.Id)
                            .SingleAsync();
                        coin.IsAsset = false;
                        context.Coins.Update(coin);
                    }
                }
                result = await context.SaveChangesAsync();
                
            }
        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }

        return result > 0;
    }
    private static void NormalizeMutationsForFee(List<Mutation> mutsNew, List<Mutation> mutsToEdit)
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
    private static object CloneMutation(Mutation mut)
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
        var context = _portfolioService.Context;
        foreach (var entry in context.ChangeTracker.Entries())
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
    private async Task<Result<List<Transaction>>> GetTransactionsByAsset(int assetId)
    {
        var context = _portfolioService.Context;
        if (assetId <= 0) { return new List<Transaction>(); }
        List<Transaction> assetTransactions = new();
        try
        {
            assetTransactions = await context.Mutations
                .Where(c => c.Asset.Id == assetId)
                .Include(t => t.Transaction)
                .ThenInclude(m => m.Mutations)
                .ThenInclude(a => a.Asset)
                .ThenInclude(ac => ac.Account)
                .GroupBy(g => g.Transaction.Id)
                .Select(grouped => new Transaction
                {
                    Id = grouped.Key,
                    RequestedAsset = grouped.Select(a => a.Asset).Where(w => w.Id == assetId).SingleOrDefault() ?? new Asset(),
                    TimeStamp = grouped.Select(t => t.Transaction.TimeStamp).SingleOrDefault(),
                    Note = grouped.Select(t => t.Transaction.Note).SingleOrDefault() ?? string.Empty,
                    Mutations = grouped.Select(t => t.Transaction.Mutations).SingleOrDefault() ?? new List<Mutation>(),
                })
                .OrderByDescending(o => o.TimeStamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            return new Result<List<Transaction>>(ex);
        }
        assetTransactions ??= new List<Transaction>();

        return assetTransactions;
    }
    private static Result<Asset> RecalculateAsset(Mutation mutation, Asset asset)
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
                    //if very small amount remains then set it to zero. Might be a precision failure
                    if (Math.Abs(asset.Qty) < 0.0001
                        && asset.Coin is not null
                        && asset.Coin.Price > 0
                        && (asset.Coin.Price * Math.Abs(asset.Qty)) < 0.01)
                    {
                        asset.Qty = 0;
                        asset.AverageCostPrice = 0;
                    }
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
        var context = _portfolioService.Context;
        Asset asset;
        try
        {
            asset = await context.Assets
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
                        //if very small amount remains then set it to zero. Might be a precision failure
                        if (Math.Abs(asset.Qty) < 0.0001
                            && asset.Coin is not null
                            && asset.Coin.Price > 0
                            && (asset.Coin.Price * Math.Abs(asset.Qty)) < 0.01)
                        {
                            asset.Qty = 0;
                            asset.AverageCostPrice = 0;
                        }
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
                    //if very small amount remains then set it to zero. Might be a precision failure
                    if (Math.Abs(asset.Qty) < 0.0001
                        && asset.Coin is not null
                        && asset.Coin.Price > 0
                        && (asset.Coin.Price * Math.Abs(asset.Qty)) < 0.01)
                    {
                        asset.Qty = 0;
                        asset.AverageCostPrice = 0;
                    }
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
        var context = _portfolioService.Context;
        Asset assetNew;
        try
        {
            if (mutationToEdit.Asset.Id != mutationNew.Asset.Id || mutationToEdit.Qty != mutationNew.Qty)
            {
                //*** FEE is always "OUT":
                //*** revert old
                var assetOld = await context.Assets
                    .Where(x => x.Id == mutationToEdit.Asset.Id)
                    .SingleAsync();

                assetOld.Qty += mutationToEdit.Qty;
                //*** apply new
                assetNew = await context.Assets
                    .Where(x => x.Coin.Symbol.ToLower() == mutationNew.Asset.Coin.Symbol.ToLower() && x.Account.Name.ToLower() == mutationNew.Asset.Account.Name.ToLower())
                    .SingleAsync();
                assetNew.Qty -= mutationNew.Qty;
                //if very small amount remains then set it to zero. Might be a precision failure
                if (Math.Abs(assetNew.Qty) < 0.0001
                    && assetNew.Coin is not null
                    && assetNew.Coin.Price > 0
                    && (assetNew.Coin.Price * Math.Abs(assetNew.Qty)) < 0.01)
                {
                    assetNew.Qty = 0;
                    assetNew.AverageCostPrice = 0;
                }
            }
            else //if nothing has changed then assure that 'assetNew' is set to its actual value instead of returning null
            {
                assetNew = await context.Assets
                    .Where(x => x.Id == mutationToEdit.Asset.Id)
                    .SingleAsync();
            }
        }
        catch (Exception ex)
        {
            return new Result<Asset>(ex);
        }
        return assetNew;
    }
    private Result<Asset> CreateNewAsset(PortfolioContext? context, Mutation mutation)
    {
        Asset asset;
        try
        {
            var coin = context.Coins.Where(x => x.Id == mutation.Asset.Coin.Id).Include(x => x.PriceLevels).Include(x => x.Narrative).First();
            var account = context.Accounts.Where(x => x.Id == mutation.Asset.Account.Id).First();

            if (mutation.Direction == MutationDirection.In) // && currentAsset == NULL
            {
                mutation.Asset.Coin = coin;
                mutation.Asset.Account = account;
                mutation.Asset.AverageCostPrice = mutation.Price;
                mutation.Asset.Qty = mutation.Qty;
                mutation.Asset.Coin.IsAsset = true;
                //** keep track of newly added asset, because this asset is not yet to be found in the dbcontext
                asset = new Asset();
                asset = mutation.Asset;
            }
            else //in case something went wrong... MutationDirection.OUT WHICH CAN'T BE
            {
                return new Result<Asset>(new InvalidOperationException());
            }
            return asset;
        }
        catch (Exception ex)
        {
            return new Result<Asset>(ex);
        }
    }
    private async Task<Result<Asset>> ReverseAndRecalculateAsset(Mutation mutation)
    {
        var context = _portfolioService.Context;
        Asset asset;
        try
        {
            asset = await context.Assets.Where(x => x.Id == mutation.Asset.Id).SingleAsync();
            switch (mutation.Direction.ToString())
            {
                case "In": //*** revert IN, so subtract instead of add
                    if (asset.Qty - mutation.Qty != 0)
                    {
                        asset.AverageCostPrice = ((asset.Qty * asset.AverageCostPrice) - (mutation.Qty * mutation.Price)) / (asset.Qty - mutation.Qty);
                        asset.Qty -= mutation.Qty;
                        //if very small amount remains then set it to zero. Might be a precision failure
                        if (Math.Abs(asset.Qty) < 0.0001
                            && asset.Coin is not null
                            && asset.Coin.Price > 0
                            && (asset.Coin.Price * Math.Abs(asset.Qty)) < 0.01)
                        {
                            asset.Qty = 0;
                            asset.AverageCostPrice = 0;
                        }
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

    public void ReloadValues()
    {
        var tempList = ListAssetTransactions;
        ListAssetTransactions = null;
        ListAssetTransactions = tempList;

    }

    private async Task UpdateMutationAndRecalculateAsset(Mutation mutationNew, Mutation mutationToEdit)
    {
        if (mutationToEdit.Type != TransactionKind.Fee)
        {
            mutationToEdit.Asset = (await ReverseAndRecalculateAsset(mutationNew, mutationToEdit))
                .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
            mutationToEdit.Qty = mutationNew.Qty;
            mutationToEdit.Price = mutationNew.Price;
        }
        else // if 'Fee'
        {
            mutationToEdit.Asset = (await ReverseAndRecalculateFee(mutationNew, mutationToEdit))
                .Match(Succ: asset => asset, Fail: err => throw new Exception(err.Message, err));
            mutationToEdit.Qty = mutationNew.Qty;
        }
    }
   
}
