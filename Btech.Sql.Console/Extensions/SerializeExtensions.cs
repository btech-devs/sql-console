using Newtonsoft.Json;

namespace Btech.Sql.Console.Extensions;

public static class SerializeExtensions
{
    public static JsonSerializerSettings DefaultJsonSerializerSettings =>
        new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

    public static string JsonSerialize<T>(
        this T entity,
        JsonSerializerSettings jsonSerializerSettings = null)
        where T : class
    {
        string serialized;

        try
        {
            serialized = JsonConvert.SerializeObject(entity, jsonSerializerSettings ?? DefaultJsonSerializerSettings);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Invalid entity. Type: '{typeof(T)}'.", e);
        }

        return serialized;
    }
}