﻿using System;
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

public partial class NarrativeService : ObservableObject, INarrativeService
{
    private readonly PortfolioService _portfolioService;
    [ObservableProperty] private static ObservableCollection<Narrative>? listNarratives;

    private SortingOrder currentSortingOrder;
    private Func<Narrative, object> currentSortFunc;

   // [ObservableProperty] public bool showOnlyAssets;

    //async partial void OnShowOnlyAssetsChanged(bool value)
    //{
    //    await PopulateNarrativesList(value);
    //}

    public NarrativeService(PortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
        currentSortFunc = x => x.TotalValue;
        currentSortingOrder = SortingOrder.Descending;
    }

    public async Task<ObservableCollection<Narrative>> PopulateNarrativesList()
    {
        var getResult = await GetNarratives();

        ListNarratives = getResult.Match(
            list => SortedList(list),
            err => new ObservableCollection<Narrative>());
        return ListNarratives;
    }

    //public async Task<ObservableCollection<Narrative>> PopulateNarrativesList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc, bool onlyAssets = false)
    public async Task PopulateNarrativesList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc)
    {
        var getResult = await GetNarratives();
        ListNarratives = getResult.Match(
            list => SortedList(list, sortingOrder, sortFunc),
            err => new ObservableCollection<Narrative>()); 
      //  return ListNarratives;
    }

    public void ClearNarrativesList()
    {
        //ListNarratives?.Clear();
        ListNarratives = null;
        OnPropertyChanged(nameof(ListNarratives));
    }

    private ObservableCollection<Narrative> SortedList(List<Narrative> list, SortingOrder sortingOrder = SortingOrder.None, Func<Narrative, object>? sortFunc = null)
    {
        if (sortingOrder != SortingOrder.None && sortFunc != null)
        {
            currentSortFunc = sortFunc;
            currentSortingOrder = sortingOrder;
        }
        return currentSortingOrder == SortingOrder.Ascending
            ? new ObservableCollection<Narrative>(list.OrderBy(currentSortFunc).ThenByDescending(x => x.IsHoldingCoins))
            : new ObservableCollection<Narrative>(list.OrderByDescending(currentSortFunc).ThenByDescending(x => x.IsHoldingCoins));
    }

    public void ReloadValues()
    {
        var tempListNarratives = ListNarratives;
        ListNarratives = null;
        ListNarratives = tempListNarratives;
    }

    public bool IsNarrativeHoldingCoins(Narrative Narrative)
    {
        if (Narrative is null || ListNarratives is null || !ListNarratives.Any()) { return false; }
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
        if (ListNarratives is null || !ListNarratives.Any()) { return Task.FromResult(false); }
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
    public Task UpdateListNarratives(Narrative? narrative)
    {
        if (ListNarratives is null || narrative is null) { return Task.FromResult(false); }
        try
        {
            var context = _portfolioService.Context;

            var narrativeToUpdateIndex = ListNarratives.IndexOf(narrative);
            var updatedNarrative = context.Narratives.AsNoTracking()
                .Where(x => x.Id == narrative.Id)
                .Include(x => x.Coins)
                .SingleOrDefault();

            ListNarratives.RemoveAt(narrativeToUpdateIndex);
            ListNarratives.Insert(narrativeToUpdateIndex, updatedNarrative);
        }
        catch (Exception) { Task.FromResult(false); }

        return Task.FromResult(true);
    }
    public async Task<Result<bool>> CreateNarrative(Narrative? newNarrative)
    {
        bool _result;
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
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
        var context = _portfolioService.Context;
        context.ChangeTracker?.Clear();
        bool result;
        if (newNarrative == null || coin == null) { return false; }
        try
        {
            var coinToUpdate = await context.Coins
                .Where(x => x.Id == coin.Id)
                .SingleAsync();

            coinToUpdate.Narrative = newNarrative;
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


    public void SortList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc)
    {
        if (ListNarratives is not null || !ListNarratives.Any())
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
        if (ListNarratives is not null || !ListNarratives.Any())
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

