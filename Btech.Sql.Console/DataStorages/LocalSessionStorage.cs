using System.Collections.Concurrent;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Models;

namespace Btech.Sql.Console.DataStorages;

/// <summary>
/// Local session storage, used for development environment and local server.
/// </summary>
public class LocalSessionStorage : RawDataSessionStorage<SessionData>
{
    public LocalSessionStorage()
    {
        this._dictionary = new();
    }

    private readonly ConcurrentDictionary<string, string> _dictionary;

    protected override Task<bool> SaveDataAsync(KeyValuePair<string, string> keyValue)
    {
        return Task.FromResult(this._dictionary.TryAdd(keyValue.Key, keyValue.Value));
    }

    protected override Task<bool> UpdateDataAsync(KeyValuePair<string, string> keyValue)
    {
        bool result = false;

        if (this._dictionary.TryGetValue(keyValue.Key, out string actualValue))
        {
            result = this._dictionary.TryUpdate(keyValue.Key, keyValue.Value, actualValue);
        }

        return Task.FromResult(result);
    }

    protected override Task<bool> DeleteDataAsync(string key)
    {
        return Task.FromResult(this._dictionary.TryRemove(key, out string _));
    }

    protected override Task<string> GetDataAsync(string key)
    {
        this._dictionary.TryGetValue(key, out string connectionString);

        return Task.FromResult(connectionString);
    }
}