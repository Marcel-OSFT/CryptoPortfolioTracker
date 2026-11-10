using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Services;

public interface INarrativeService
{
    public Task<Result<bool>> CreateNarrative(Narrative? newNarrative);
    public Task<Result<bool>> EditNarrative(Narrative newNarrative, Narrative NarrativeToEdit);
    public Task<Result<bool>> RemoveNarrative(int NarrativeId);
    public Task<Result<bool>> NarrativeHasNoCoins(int assetId);
    Task<ObservableCollection<Narrative>> PopulateNarrativesList();
    Task<bool> RemoveFromListNarratives(int NarrativeId);
    Task<bool> AddToListNarratives(Narrative? newNarrative);
    Task<ObservableCollection<Narrative>> UpdateListNarratives(Narrative? narrative);
    Narrative GetNarrativeByCoin(Coin coin);
    Task<Result<List<Narrative>>> GetNarratives();
    Task<Result<bool>> AssignNarrative(Coin coin, Narrative newNarrative);
    bool DoesNarrativeNameExist(string name);
    Narrative GetDefaultNarrative();
    void ClearNarrativesList();
}

public partial class NarrativeService : ObservableObject, INarrativeService
{
    private readonly IAppState _appState;
    private PortfolioContext Context => _appState.Context;
    private ObservableCollection<Narrative>? ListNarratives {  get; set; } = new ObservableCollection<Narrative>();

    private SortingOrder currentSortingOrder;
    private Func<Narrative, object> currentSortFunc;

    public NarrativeService(IAppState appState)
    {
        _appState = appState;
        _appState.ContextChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Context));
        };
    }

    public async Task<ObservableCollection<Narrative>> PopulateNarrativesList()
    {
        var getResult = await GetNarratives();

        ListNarratives = getResult.Match(
            list => new ObservableCollection<Narrative>(list),
            err => new ObservableCollection<Narrative>());
        return ListNarratives;
    }

    public void ClearNarrativesList()
    {
        ListNarratives = null;
        OnPropertyChanged(nameof(ListNarratives));
    }

   
    
    public async Task<bool> RemoveFromListNarratives(int NarrativeId)
    {
        if (ListNarratives is null || !ListNarratives.Any()) { return false; }
        try
        {
            var Narrative = ListNarratives.Where(x => x.Id == NarrativeId).Single();
            ListNarratives.Remove(Narrative);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }
    public async Task<bool> AddToListNarratives(Narrative? newNarrative)
    {
        if (ListNarratives is null || newNarrative is null) { return false; }
        try
        {
            ListNarratives.Add(newNarrative);
        }
        catch (Exception) { return false; }

        return true;
    }
    public async Task<ObservableCollection<Narrative>> UpdateListNarratives(Narrative? narrative)
    {
        ObservableCollection<Narrative> updated = new();

        if (ListNarratives is null || !ListNarratives.Any() || narrative is null) { return updated; }
        try
        {
            var context = Context;

            var narrativeToUpdate = ListNarratives.Where(x => x.Id == narrative.Id).Single();
            var updatedNarrative = context.Narratives.AsNoTracking()
                .Include(i => i.Coins)
                .Where(x => x.Id == narrative.Id)
                .SingleOrDefault();

            var index = ListNarratives.IndexOf(narrativeToUpdate);
            ListNarratives[index].Name = updatedNarrative?.Name;
            ListNarratives[index].About = updatedNarrative?.About;
            updated.Add(updatedNarrative);
        }
        catch (Exception) { return new ObservableCollection<Narrative>(); }

        return updated;
    }
    public async Task<Result<bool>> CreateNarrative(Narrative? newNarrative)
    {
        bool _result;
        var context = Context;
        context.ChangeTracker?.Clear();

        if (newNarrative == null) { return false; }
        try
        {
            context.Narratives.Add(newNarrative);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _result;
    }
    public async Task<Result<bool>> EditNarrative(Narrative newNarrative, Narrative NarrativeToEdit)
    {
        var context = Context;
        context.ChangeTracker?.Clear();
        bool _result;
        if (newNarrative == null || newNarrative.Name == "" || NarrativeToEdit == null) { return false; }
        try
        {
            var Narrative = await context.Narratives
                .Where(x => x.Id == NarrativeToEdit.Id)
                .SingleAsync();

            Narrative.Name = newNarrative.Name;
            Narrative.About = newNarrative.About;
            context.Narratives.Update(Narrative);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _result;
    }
    public async Task<Result<bool>> RemoveNarrative(int NarrativeId)
    {
        var context = Context;
        context.ChangeTracker?.Clear();
        bool _result;
        if (NarrativeId <= 0) { return false; }
        try
        {
            var Narrative = await context.Narratives.Where(x => x.Id == NarrativeId).SingleAsync();
            context.Narratives.Remove(Narrative);
            _result = await context.SaveChangesAsync() > 0;
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        finally
        {
            context.ChangeTracker?.Clear();
        }
        return _result;
    }
    public async Task<Result<List<Narrative>>> GetNarratives()
    {
        var context = Context;
        List<Narrative> Narratives = null;
        try
        {
            Narratives = await context.Narratives
                .AsNoTracking()
                .Include(x => x.Coins)
                .ToListAsync();

            if (Narratives == null || Narratives.Count == 0) { return new List<Narrative>(); }

            foreach (var narrative in Narratives)
            {
                narrative.TotalValue = await CalculateTotalValue(narrative);
                narrative.CostBase = await CalculateCostBase(narrative);
                narrative.IsHoldingCoins = narrative.Coins != null && narrative.Coins.Count > 0;
            }
        }
        catch (Exception ex)
        {
            return new Result<List<Narrative>>(ex);
        }
        return Narratives;
    }

    public async Task<Result<bool>> NarrativeHasNoCoins(int NarrativeId)
    {
        var context = Context;
        Narrative Narrative;
        if (NarrativeId <= 0) { return false; }
        try
        {
            Narrative = await context.Narratives
                .AsNoTracking()
                .Where(c => c.Id == NarrativeId)
                .Include(x => x.Coins)
                .SingleAsync();
        }
        catch (Exception ex)
        {
            return new Result<bool>(ex);
        }
        return Narrative.Coins == null || Narrative.Coins.Count == 0;
    }

    public async Task<double> CalculateTotalValue(Narrative narrative)
    {
        var context = Context;
        if (narrative == null || narrative.Coins == null || !narrative.Coins.Any()) return 0.0;

        var sum = await context.Assets
            .AsNoTracking()
            .Where(asset => narrative.Coins.Contains(asset.Coin))        
            .Include(x => x.Coin)
            .SumAsync(asset => asset.Qty * asset.Coin.Price);

        return sum;
    }

    public async Task<double> CalculateCostBase(Narrative narrative)
    {
        var context = Context;
        if (narrative == null || narrative.Coins == null || !narrative.Coins.Any()) return 0.0;

        var sum = await context.Assets
            .AsNoTracking()
            .Where(asset => narrative.Coins.Contains(asset.Coin))
            .Include(x => x.Coin)
            .SumAsync(asset => asset.Qty * asset.AverageCostPrice);

        return sum;
    }

    public Narrative GetDefaultNarrative()
    {
        var context = Context;
        Narrative narrative;
        try
        {
            narrative = context.Narratives
                .AsNoTracking()
                .Where(x => x.Name == "- Not Assigned -")
                .Include(x => x.Coins)
                .Single();
        }
        catch (Exception ex)
        {
            narrative = new();
        }
        return narrative;
    }

    public Narrative GetNarrativeByCoin(Coin coin)
    {
        var context = Context;
        Narrative narrative;
        try
        {
            narrative = context.Narratives
                .AsNoTracking()
                .Where(x => x.Coins.Contains(coin))
                .First();
        }
        catch (Exception)
        {
            narrative = new Narrative();
        }
        return narrative;
    }
    public bool DoesNarrativeNameExist(string name)
    {
        var context = Context;
        Narrative? narrative;
        try
        {
            narrative = context.Narratives
                .AsNoTracking()
                .Where(x => x.Name.ToLower() == name.ToLower())
                .FirstOrDefault();
        }
        catch (Exception)
        {
            narrative = null;
        }
        return narrative != null;
    }

    public async Task<Result<bool>> AssignNarrative(Coin coin, Narrative newNarrative)
    {
        var context = Context;
        context.ChangeTracker?.Clear();
        bool result;
        if (newNarrative == null || coin == null) { return false; }
        try
        {
            var narrative = await context.Narratives.FirstOrDefaultAsync(x => x.Name == newNarrative.Name);

            var coinToUpdate = await context.Coins
                .Where(x => x.Id == coin.Id)
                .SingleAsync();

            coinToUpdate.Narrative = narrative;
            context.Update(coinToUpdate);

            result = await context.SaveChangesAsync() > 0;
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
        return result;
    }

    private void RejectChanges()
    {
        var context = Context;
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


}

