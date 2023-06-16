using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Base;

/// <summary>
/// Represents an abstract class that provides storage for sessions of a generic type T.
/// </summary>
/// <typeparam name="T">The type of data to be stored in the session.</typeparam>
public abstract class RawDataSessionStorage<T> : ISessionStorage<T>
    where T : class
{
    #region Public Methods

    /// <summary>
    /// Public interface for thread-safety writing.
    /// </summary>
    /// <param name="email">String representation of record key.</param>
    /// <param name="sessionData">Record value.</param>
    /// <returns>Returns 'true' if writing was successful.</returns>
    public async Task<bool> SaveAsync(string email, T sessionData)
    {
        string value;

        if (email.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(email), "Value can not be null.");
        }

        if (sessionData is null)
        {
            throw new ArgumentNullException(nameof(sessionData), "Value can not be null.");
        }

        if (sessionData is string strObject)
        {
            value = strObject;
        }
        else if (typeof(T).IsClass)
        {
            value = JsonConvert.SerializeObject(sessionData);
        }
        else
        {
            throw new UnsupportedContentTypeException($"Type: {typeof(T).Name} is not supported.");
        }

        return await this.SaveDataAsync(new KeyValuePair<string, string>(email, value));
    }

    /// <summary>
    /// Public interface for thread-safety updating.
    /// </summary>
    /// <param name="email">String representation of record key.</param>
    /// <param name="updatedSessionData">New record value.</param>
    /// <returns>Returns 'true' if record was found and updated successfully, otherwise 'false'.</returns>
    public async Task<bool> UpdateAsync(string email, T updatedSessionData)
    {
        string value;

        if (email.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(email), "Value can not be null.");
        }

        if (updatedSessionData is null)
        {
            throw new ArgumentNullException(nameof(updatedSessionData), "Value can not be null.");
        }

        if (updatedSessionData is string strObject)
        {
            value = strObject;
        }
        else if (typeof(T).IsClass)
        {
            value = JsonConvert.SerializeObject(updatedSessionData);
        }
        else
        {
            throw new UnsupportedContentTypeException($"Type: {typeof(T).Name} is not supported.");
        }

        return await this.UpdateDataAsync(new KeyValuePair<string, string>(email, value));
    }

    /// <summary>
    /// Public interface for thread-safety deleting.
    /// </summary>
    /// <param name="email">String representation of record key.</param>
    /// <returns>Returns 'true' if deleting was successful.</returns>
    public async Task<bool> DeleteAsync(string email)
    {
        return await this.DeleteDataAsync(email);
    }

    /// <summary>
    /// Public interface for thread-safety reading.
    /// </summary>
    /// <param name="email">String representation of record key.</param>
    /// <returns>Returns serialized object value or 'null'.</returns>
    public async Task<T> GetAsync(string email)
    {
        string serializedObject = await this.GetDataAsync(email);

        T deserializedObject = null;

        if (serializedObject != null)
        {
            if (typeof(T).FullName == "System.String")
            {
                deserializedObject = (T) Convert.ChangeType(serializedObject, typeof(T));
            }
            else if (typeof(T).IsClass)
            {
                deserializedObject = JsonConvert.DeserializeObject<T>(serializedObject);
            }
        }

        return deserializedObject;
    }

    #endregion Public Methods

    #region Abstract Methods

    /// <summary>
    /// The main write method, which performs the necessary object transformations and accesses the repository.
    /// </summary>
    /// <param name="keyValue">Key-value pair, representing the object.</param>
    /// <returns>Returns 'true' if writing was successful.</returns>
    protected abstract Task<bool> SaveDataAsync(KeyValuePair<string, string> keyValue);

    /// <summary>
    /// The main update method, which performs the necessary object transformations and accesses the repository.
    /// </summary>
    /// <param name="keyValue">Key-value pair, representing the object.</param>
    /// <returns>Returns 'true' if record was found and updated successfully, otherwise 'false'.</returns>
    protected abstract Task<bool> UpdateDataAsync(KeyValuePair<string, string> keyValue);

    /// <summary>
    /// The main remove method that searches for an object by the key and accesses the repository for deleting.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>Returns 'true' if removing was successful.</returns>
    protected abstract Task<bool> DeleteDataAsync(string key);

    /// <summary>
    /// The main read method that searches for an object by the key and accesses the repository for object getting.
    /// </summary>
    /// <param name="key">String representation of the key.</param>
    /// <returns>Returns serialized object.</returns>
    protected abstract Task<string> GetDataAsync(string key);

    #endregion Abstract Methods
}