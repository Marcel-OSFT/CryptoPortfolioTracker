using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using LanguageExt.Common;

namespace CryptoPortfolioTracker.Services;

public interface INarrativeService
{
    public Task<Result<bool>> CreateNarrative(Narrative? newNarrative);
    public Task<Result<bool>> EditNarrative(Narrative newNarrative, Narrative NarrativeToEdit);
    public Task<Result<bool>> RemoveNarrative(int NarrativeId);
    public Task<Result<Narrative>> GetNarrativeByName(string name);
    public Task<Result<bool>> NarrativeHasNoCoins(int assetId);
    Task<ObservableCollection<Narrative>> PopulateNarrativesList();
    bool IsNarrativeHoldingCoins(Narrative Narrative);
    Task RemoveFromListNarratives(int NarrativeId);
    Task AddToListNarratives(Narrative? newNarrative);
    void ReloadValues();
    Narrative GetNarrativeByCoin(Coin coin);
    Task<Result<List<Narrative>>> GetNarratives();
    Task<Result<bool>> AssignNarrative(Coin coin, Narrative newNarrative);
    bool DoesNarrativeNameExist(string name);
}
