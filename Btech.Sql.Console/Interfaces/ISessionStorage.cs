namespace Btech.Sql.Console.Interfaces;

/// <summary>
/// Interface for session storage.
/// </summary>
/// <typeparam name="TEntity">The type of session entity.</typeparam>
public interface ISessionStorage<TEntity> where TEntity : class
{
    /// <summary>
    /// Save session data asynchronously.
    /// </summary>
    /// <param name="email">The email of the user associated with the session.</param>
    /// <param name="data">The session data to save.</param>
    /// <returns>True if the data is successfully saved; otherwise, false.</returns>
    Task<bool> SaveAsync(string email, TEntity data);

    /// <summary>
    /// Update session data asynchronously.
    /// </summary>
    /// <param name="email">The email of the user associated with the session.</param>
    /// <param name="updatedSessionData">The updated session data.</param>
    /// <returns>True if the data is successfully updated; otherwise, false.</returns>
    Task<bool> UpdateAsync(string email, TEntity updatedSessionData);

    /// <summary>
    /// Delete session data asynchronously.
    /// </summary>
    /// <param name="email">The email of the user associated with the session.</param>
    /// <returns>True if the data is successfully deleted; otherwise, false.</returns>
    Task<bool> DeleteAsync(string email);

    /// <summary>
    /// Retrieve session data asynchronously.
    /// </summary>
    /// <param name="email">The email of the user associated with the session.</param>
    /// <returns>The session data if it exists; otherwise, null.</returns>
    Task<TEntity> GetAsync(string email);
}