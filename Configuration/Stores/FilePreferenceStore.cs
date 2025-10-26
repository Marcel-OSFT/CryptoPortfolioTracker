using System.Collections.Concurrent;
using System.Text.Json;

namespace CryptoPortfolioTracker.Configuration.Stores;

public class FilePreferenceStore : IPreferenceStore
{
    private static readonly ConcurrentDictionary<string, object?> _store = new();
    private static readonly string _filePath = Path.Combine(AppConstants.AppDataPath, "prefs_store.json");
    private static readonly SemaphoreSlim _fileLock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    // track last save task so callers can await completion
    private static Task? _lastSaveTask;

    public FilePreferenceStore()
    {
        LoadFromFile();
    }

    public void Set<T>(string key, T value)
    {
        _store[key] = value;
        // keep the Task so callers can await it
        _lastSaveTask = SaveToFileAsync();
    }

    public T? Get<T>(string key, T? defaultValue = default)
    {
        if (!_store.TryGetValue(key, out var raw) || raw is null) return defaultValue;
        if (raw is T t) return t;

        try
        {
            // attempt round-trip conversion
            var json = JsonSerializer.Serialize(raw, _jsonOptions);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public bool Contains(string key) => _store.ContainsKey(key);

    public void Remove(string key)
    {
        _store.TryRemove(key, out _);
        _lastSaveTask = SaveToFileAsync();
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        // return the latest save task if there is one, otherwise completed task
        var t = _lastSaveTask;
        return t ?? Task.CompletedTask;
    }

    private record PersistEntry(string TypeAssemblyQualifiedName, string Json);

    private async Task SaveToFileAsync()
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var data = new Dictionary<string, PersistEntry>(_store.Count);
            foreach (var kv in _store)
            {
                var value = kv.Value;
                if (value == null)
                {
                    data[kv.Key] = new PersistEntry(typeof(object).AssemblyQualifiedName!, "null");
                    continue;
                }

                var typeName = value.GetType().AssemblyQualifiedName!;
                var json = JsonSerializer.Serialize(value, value.GetType(), _jsonOptions);
                data[kv.Key] = new PersistEntry(typeName, json);
            }

            var fileJson = JsonSerializer.Serialize(data, _jsonOptions);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath) ?? ".");
            await File.WriteAllTextAsync(_filePath, fileJson).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private void LoadFromFile()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var fileJson = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, PersistEntry>>(fileJson, _jsonOptions);
            if (data == null) return;

            foreach (var kv in data)
            {
                try
                {
                    var entry = kv.Value;
                    var type = Type.GetType(entry.TypeAssemblyQualifiedName) ?? typeof(string);
                    if (entry.Json == "null")
                    {
                        _store[kv.Key] = null;
                        continue;
                    }
                    var obj = JsonSerializer.Deserialize(entry.Json, type, _jsonOptions);
                    _store[kv.Key] = obj!;
                }
                catch
                {
                    // ignore individual entry errors
                }
            }
        }
        catch
        {
            // ignore load errors - start with empty store
        }
    }
}