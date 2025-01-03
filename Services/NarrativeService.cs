using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Infrastructure;
using CryptoPortfolioTracker.Models;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.EntityFrameworkCore;

namespace CryptoPortfolioTracker.Services;

public partial class NarrativeService : ObservableObject, INarrativeService
{
    private readonly PortfolioContext context;
    [ObservableProperty] private static ObservableCollection<Narrative>? listNarratives;

    private SortingOrder currentSortingOrder;
    private Func<Narrative, object> currentSortFunc;


    public NarrativeService(PortfolioContext portfolioContext)
    {
        context = portfolioContext;

        currentSortFunc = x => x.TotalValue;
        currentSortingOrder = SortingOrder.Descending;

    }
    public async Task<ObservableCollection<Narrative>> PopulateNarrativesList()
    {
        var getNarrativesResult = await GetNarratives();
        getNarrativesResult.IfSucc(list =>
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListNarratives = new(list.OrderBy(currentSortFunc).ThenByDescending(x => x.Coins.Count));
            }
            else
            {
                ListNarratives = new(list.OrderByDescending(currentSortFunc).ThenByDescending(x => x.Coins.Count));
            }

            //ListNarratives = new ObservableCollection<Narrative>(s
            //    .OrderByDescending(x => x.TotalValue)
            //    .ThenByDescending(x => x.Coins.Count));
        });
        getNarrativesResult.IfFail(err => ListNarratives = new());
        return ListNarratives;
    }

    public async Task<ObservableCollection<Narrative>> PopulateNarrativesList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc)
    {
        var getNarrativesResult = await GetNarratives();
        getNarrativesResult.IfSucc(list =>
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListNarratives = new(list.OrderBy(sortFunc));
            }
            else
            {
                ListNarratives = new(list.OrderByDescending(sortFunc));
            }
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;

        });
        getNarrativesResult.IfFail(err => ListNarratives = new());
        return ListNarratives;
    }




    public void ReloadValues()
    {
        var tempListNarratives = ListNarratives;
        ListNarratives = null;
        ListNarratives = tempListNarratives;
    }

    public bool IsNarrativeHoldingCoins(Narrative Narrative)
    {
        if (Narrative is null || ListNarratives is null) { return false; }
        var result = false;
        try
        {
            result = ListNarratives.Where(x => x.Id == Narrative.Id).Single().IsHoldingCoins;
        }
        catch (Exception)
        {
            //Element just removed from the list...
        }
        return result;
    }
    public Task RemoveFromListNarratives(int NarrativeId)
    {
        if (ListNarratives is null) { return Task.FromResult(false); }
        try
        {
            var Narrative = ListNarratives.Where(x => x.Id == NarrativeId).Single();
            ListNarratives.Remove(Narrative);
        }
        catch (Exception)
        {
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
    public Task AddToListNarratives(Narrative? newNarrative)
    {
        if (ListNarratives is null || newNarrative is null) { return Task.FromResult(false); }
        try
        {
            ListNarratives.Add(newNarrative);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }
    public async Task<Result<bool>> CreateNarrative(Narrative? newNarrative)
    {
        bool _result;

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
        return _result;
    }
    public async Task<Result<bool>> EditNarrative(Narrative newNarrative, Narrative NarrativeToEdit)
    {
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
        return _result;
    }
    public async Task<Result<bool>> RemoveNarrative(int NarrativeId)
    {
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
        return _result;
    }
    public async Task<Result<List<Narrative>>> GetNarratives()
    {
        List<Narrative> Narratives = null;
        try
        {
            Narratives = await context.Narratives
                .Include(x => x.Coins)
                .OrderBy(x => x.Name)
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
    public async Task<Result<Narrative>> GetNarrativeByName(string name)
    {
        Narrative narrative = null;
        try
        {
            narrative = await context.Narratives
                .Include(x => x.Coins)
                .Where(x => x.Name.ToLower() == name.ToLower())
                .SingleAsync();

            narrative.TotalValue = await CalculateTotalValue(narrative); ;
            narrative.IsHoldingCoins = narrative.Coins != null && narrative.Coins.Count > 0;
        }
        catch (Exception ex)
        {
            return new Result<Narrative>(ex);
        }
        return narrative;
    }
    public async Task<Result<bool>> NarrativeHasNoCoins(int NarrativeId)
    {
        Narrative Narrative;
        if (NarrativeId <= 0) { return false; }
        try
        {
            Narrative = await context.Narratives
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
        if (narrative == null || narrative.Coins == null || !narrative.Coins.Any()) return 0.0;

        var sum = await context.Assets
            .Include(x => x.Coin)
            .Where(asset => narrative.Coins.Contains(asset.Coin))
            
            .SumAsync(asset => asset.Qty * asset.Coin.Price);

        return sum;
    }

    public async Task<double> CalculateCostBase(Narrative narrative)
    {
        if (narrative == null || narrative.Coins == null || !narrative.Coins.Any()) return 0.0;

        var sum = await context.Assets
            .Include(x => x.Coin)
            .Where(asset => narrative.Coins.Contains(asset.Coin))

            .SumAsync(asset => asset.Qty * asset.AverageCostPrice);

        return sum;
    }

    public Narrative GetNarrativeByCoin(Coin coin)
    {
        Narrative narrative;
        try
        {
            narrative = context.Narratives.Where(x => x.Coins.Contains(coin)).First();
        }
        catch (Exception)
        {
            narrative = new Narrative();
        }
        return narrative;
    }
    public bool DoesNarrativeNameExist(string name)
    {
        Narrative narrative;
        try
        {
            narrative = context.Narratives.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
        }
        catch (Exception)
        {
            narrative = new Narrative();
        }
        return narrative != null;
    }

    public async Task<Result<bool>> AssignNarrative(Coin coin, Narrative newNarrative)
    {
        bool result;
        if (newNarrative == null || coin == null) { return false; }
        try
        {
            //var previousNarrative = context.Narratives.Where(x => x.Coins.Contains(coin)).Single();
            //if (previousNarrative == newNarrative) return true;

            //previousNarrative.Coins.Remove(coin);
            //newNarrative.Coins.Add(coin);
            //result = await context.SaveChangesAsync() > 0;

            var coinToUpdate = await context.Coins
                .Where(x => x.Id == coin.Id)
                .SingleAsync();

            coinToUpdate.Narrative = newNarrative;


            result = await context.SaveChangesAsync() > 0;


        }
        catch (Exception ex)
        {
            RejectChanges();
            return new Result<bool>(ex);
        }
        return result;
    }

    private void RejectChanges()
    {
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


    public void SortList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc)
    {
        if (ListNarratives is not null)
        {
            if (sortingOrder == SortingOrder.Ascending)
            {
                ListNarratives = new ObservableCollection<Narrative>(ListNarratives.OrderBy(sortFunc));
            }
            else
            {
                ListNarratives = new ObservableCollection<Narrative>(ListNarratives.OrderByDescending(sortFunc));
            }
        }
        currentSortingOrder = sortingOrder;
        currentSortFunc = sortFunc;
    }
    /// <summary>
    /// this function without parameters will sort the list using the last used settings.
    /// </summary>
    public void SortList()
    {
        if (ListNarratives is not null)
        {
            if (currentSortingOrder == SortingOrder.Ascending)
            {
                ListNarratives = new ObservableCollection<Narrative>(ListNarratives.OrderBy(currentSortFunc));
            }
            else
            {
                ListNarratives = new ObservableCollection<Narrative>(ListNarratives.OrderByDescending(currentSortFunc));
            }
        }
    }




}

