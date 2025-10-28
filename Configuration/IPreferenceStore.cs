
namespace CryptoPortfolioTracker.Configuration;

public interface IPreferenceStore
{
    void Set<T>(string key, T value);
    T? Get<T>(string key, T? defaultValue = default);
    bool Contains(string key);
    void Remove(string key);
    Task FlushAsync(CancellationToken cancellationToken = default);
}