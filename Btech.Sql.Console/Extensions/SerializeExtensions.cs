using Newtonsoft.Json;

namespace Btech.Sql.Console.Extensions;

/// <summary>
/// Provides extension methods for serializing.
/// </summary>
public static class SerializeExtensions
{
    /// <summary>
    /// Gets the default <see cref="JsonSerializerSettings"/> used for serialization.
    /// </summary>
    public static JsonSerializerSettings DefaultJsonSerializerSettings =>
        new()
        {
            NullValueHandling = NullValueHandling.Ignore
        };

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="entity">The object to serialize.</param>
    /// <param name="jsonSerializerSettings">Optional <see cref="JsonSerializerSettings"/> used for serialization. If null, <see cref="DefaultJsonSerializerSettings"/> will be used.</param>
    /// <returns>The serialized JSON string.</returns>
    /// <exception cref="ArgumentException">Thrown if the object cannot be serialized.</exception>
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