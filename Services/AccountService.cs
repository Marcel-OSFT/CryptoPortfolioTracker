using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace CryptoPortfolioTracker.Services;

public interface IAccountService
{
    public Task<Result<bool>> CreateAccount(Account? newAccount);
    public Task<Result<bool>> EditAccount(Account newAccount, Account accountToEdit);
    public Task<Result<bool>> RemoveAccount(int accountId);
    public Task<Result<bool>> AccountHasNoAssets(int assetId);
    Task<ObservableCollection<Account>> PopulateAccountsList();
    Task<ObservableCollection<AssetAccount>> PopulateAccountsByAssetList(int coinId);
    Task<Result<AssetAccount>> GetAccountByAsset(int assetId);
    Task<bool> RemoveFromListAccounts(int accountId);
    Task<bool> AddToListAccounts(Account? newAccount);
    Task<ObservableCollection<Account>> UpdateListAccounts(Account? account);
    Task<ObservableCollection<AssetAccount>> UpdateListAssetAccount(AssetAccount accountAffected);
    void ClearAccountsByAssetList();
    AssetAccount GetAffectedAccount(Transaction transaction);
    bool DoesAccountNameExist(string name);
    void ClearAccountsList();
}

public partial class AccountService : ObservableObject, IAccountService
{
    private readonly IAppState _appState;
    private PortfolioContext Context => _appState.Context;
    private ObservableCollection<Account>? ListAccounts { get; set; } = new ObservableCollection<Account>();
    private ObservableCollection<AssetAccount>? ListAssetAccounts { get; set; } = new();

    public AccountService(IAppState appState)
    {
        _appState = appState;
        _appState.ContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Context));
        };
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

    public async Task<ObservableCollection<AssetAccount>> PopulateAccountsByAssetList(int coinId)
    {
        var getResult = await GetAccountsByAsset(coinId);
        ListAssetAccounts = getResult.Match(
            list => new ObservableCollection<AssetAccount>(list.OrderByDescending(x => x.Qty)),
            err => new ObservableCollection<AssetAccount>()
        );

        return ListAssetAccounts;
    }

    public void ClearAccountsList()
    {
        ListAccounts = null;
        OnPropertyChanged(nameof(ListAccounts));
    }

    public void ClearAccountsByAssetList()
    {
        ListAssetAccounts = null;
        OnPropertyChanged(nameof(ListAssetAccounts));
    }
    public AssetAccount GetAffectedAccount(Transaction transaction)
    {
        if (ListAssetAccounts == null && transaction.RequestedAsset == null)
        {
            throw new InvalidOperationException("ListAssetAccounts or RequestedAsset is null");
        }

        return ListAssetAccounts.Where(t => t.AssetId == transaction.RequestedAsset.Id).Single();
    }

    public async Task<bool> RemoveFromListAccounts(int accountId)
    {
        if (ListAccounts is null || !ListAccounts.Any()) { return false; }
        try
        {
            var account = ListAccounts.Where(x => x.Id == accountId).Single();
            ListAccounts.Remove(account);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }
    public async Task<bool> AddToListAccounts(Account? newAccount)
    {
        if (ListAccounts is null || !ListAccounts.Any() || newAccount is null) { return false; }
        try
        {
            ListAccounts.Add(newAccount);
        }
        catch (Exception) { return false; }

        return true;
    }
    public async Task<ObservableCollection<Account>> UpdateListAccounts(Account? account)
    {
        ObservableCollection<Account> updated = new();

        if (ListAccounts is null || !ListAccounts.Any() || account is null) { return updated; }
        try
        {
            var context = Context;

            var accountToUpdate = ListAccounts.Where(x => x.Id == account.Id).Single();
            var updatedAccount = context.Accounts.AsNoTracking()
                .Include(x => x.Assets)
                    .ThenInclude(c => c.Coin)
                .Where(x => x.Id == account.Id)
                .SingleOrDefault();

            var index = ListAccounts.IndexOf(accountToUpdate);
            ListAccounts[index].Name = updatedAccount?.Name;
            ListAccounts[index].About = updatedAccount?.About;

            updated.Add(updatedAccount);
        }
        catch (Exception) { return new ObservableCollection<Account>(); }

        return updated;
    }
    public async Task<ObservableCollection<AssetAccount>> UpdateListAssetAccount(AssetAccount accountAffected)
    {
        ObservableCollection<AssetAccount> updated = new();

        if (ListAssetAccounts is null || !ListAssetAccounts.Any() || accountAffected == null) return updated;

        var result = await GetAccountByAsset(accountAffected.AssetId);
        if (result.IsFaulted) return updated;

        var updatedAccount = result.IfFail(new AssetAccount());
        if (updatedAccount == null || string.IsNullOrEmpty(updatedAccount.Name)) return updated;

        var index = ListAssetAccounts.IndexOf(ListAssetAccounts.FirstOrDefault(a => a.Name == accountAffected.Name));
        if (index == -1) return updated;

        ListAssetAccounts[index] = updatedAccount;
        updated.Add(updatedAccount);

        return updated;
    }
    public async Task<Result<AssetAccount>> GetAccountByAsset(int assetId)
    {
        var context = Context;
        if (assetId <= 0) { return new AssetAccount(); }
        AssetAccount? assetAccount;
        try
        {
            assetAccount = await context.Assets
                .AsNoTracking()
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
        var context = Context;
        context.ChangeTracker.Clear();
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
        finally
        {
            context.ChangeTracker.Clear();
        }
        return _result;
    }
    public async Task<Result<bool>> EditAccount(Account newAccount, Account accountToEdit)
    {
        var context = Context;
        context.ChangeTracker.Clear();
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
        finally
        {
            context.ChangeTracker.Clear();
        }
        return _result;
    }
    public async Task<Result<bool>> RemoveAccount(int accountId)
    {
        var context = Context;
        context.ChangeTracker.Clear();
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
        finally
        {
            context.ChangeTracker.Clear();
        }
        return _result;
    }
    private async Task<Result<List<Account>>> GetAccounts()
    {
        var context = Context;
        List<Account> accounts;
        try
        {
            accounts = await context.Accounts
                .AsNoTracking()
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

    public async Task<Result<bool>> AccountHasNoAssets(int accountId)
    {
        var context = Context;
        Account account;
        if (accountId <= 0) { return false; }
        try
        {
            account = await context.Accounts
                .AsNoTracking()
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
        var context = Context;
        if (coinId <= 0) { return new List<AssetAccount>(); }
        List<AssetAccount> assetAccounts;
        try
        {
            assetAccounts = await context.Assets
                .AsNoTracking()
                .Where(c => c.Coin.Id == coinId)
                .Include(x => x.Coin)
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
        var context = Context;
        try
        {
            return context.Accounts
                .AsNoTracking()
                .Any(x => x.Name.ToLower() == name.ToLower());
        }
        catch (Exception)
        {
            return false;
        }
    }
}

