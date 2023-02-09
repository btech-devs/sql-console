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

    private readonly Dictionary<string, string> _dictionary;

    public override Task<long> CountAsync() => Task.FromResult<long>(this._dictionary.Count);

    protected override Task<bool> SaveDataAsync(KeyValuePair<string, string> keyValue)
    {
        this._dictionary.Add(keyValue.Key, keyValue.Value);

        return Task.FromResult(true);
    }

    protected override Task<bool> UpdateDataAsync(KeyValuePair<string, string> keyValue)
    {
        bool result = false;

        if (this._dictionary.TryGetValue(keyValue.Key, out string _))
        {
            this._dictionary[keyValue.Key] = keyValue.Value;
            result = true;
        }

        return Task.FromResult(result);
    }

    protected override Task<bool> DeleteDataAsync(string key)
    {
        this._dictionary.Remove(key);

        return Task.FromResult(true);
    }

    protected override Task<string> GetDataAsync(string key)
    {
        this._dictionary.TryGetValue(key, out string connectionString);

        return Task.FromResult(connectionString);
    }
}