using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Controls;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface INarrativeService
{
    bool ShowOnlyAssets { get; set; }
    public Task<Result<bool>> CreateNarrative(Narrative? newNarrative);
    public Task<Result<bool>> EditNarrative(Narrative newNarrative, Narrative NarrativeToEdit);
    public Task<Result<bool>> RemoveNarrative(int NarrativeId);
   // public Task<Result<Narrative>> GetNarrativeByName(string name);
    public Task<Result<bool>> NarrativeHasNoCoins(int assetId);
    Task<ObservableCollection<Narrative>> PopulateNarrativesList(bool onlyAssets = false);
    bool IsNarrativeHoldingCoins(Narrative Narrative);
    Task RemoveFromListNarratives(int NarrativeId);
    Task AddToListNarratives(Narrative? newNarrative);
    Task UpdateListNarratives(Narrative? narrative);
    void ReloadValues();
    Narrative GetNarrativeByCoin(Coin coin);
    Task<Result<List<Narrative>>> GetNarratives(bool onlyAssets = false);
    Task<Result<bool>> AssignNarrative(Coin coin, Narrative newNarrative);
    bool DoesNarrativeNameExist(string name);
    void SortList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc);
    void SortList();
    Task PopulateNarrativesList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc, bool onlyAssets = false);
    //Task<ObservableCollection<Narrative>> PopulateNarrativesList(SortingOrder sortingOrder, Func<Narrative, object> sortFunc, bool onlyAssets = false);
    Narrative GetDefaultNarrative();
    void ClearNarrativesList();
}
