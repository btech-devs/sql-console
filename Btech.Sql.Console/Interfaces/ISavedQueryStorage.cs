using Btech.Sql.Console.Models;

namespace Btech.Sql.Console.Interfaces;

/// <summary>
/// Represents a storage interface for saving and retrieving user query data.
/// </summary>
public interface ISavedQueryStorage
{
    /// <summary>
    /// Saves a query data object for the specified email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="data">The query data to save.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains a value indicating whether the save operation was successful.</returns>
    Task<bool> SaveAsync(string email, QueryData data);

    /// <summary>
    /// Updates an existing query data object for the specified email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="data">The updated query data.</param>
    /// <returns>A task that represents the asynchronous update operation. The task result contains a value indicating whether the update operation was successful.</returns>
    Task<bool> UpdateAsync(string email, QueryData data);

    /// <summary>
    /// Deletes a query data object for the specified email address and ID.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="id">The ID of the query data object to delete. If null, all query data objects for the specified email address are deleted.</param>
    /// <returns>A task that represents the asynchronous delete operation. The task result contains a value indicating whether the delete operation was successful.</returns>
    Task<bool> DeleteAsync(string email, long? id = null);

    /// <summary>
    /// Gets a list of all query data objects for the specified email address.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <returns>A task that represents the asynchronous retrieval operation. The task result contains a list of all query data objects for the specified email address.</returns>
    Task<List<QueryData>> GetAsync(string email);

    /// <summary>
    /// Gets a query data object for the specified email address and ID.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="id">The ID of the query data object to retrieve.</param>
    /// <returns>A task that represents the asynchronous retrieval operation. The task result contains the query data object with the specified ID.</returns>
    Task<QueryData> GetAsync(string email, long id);
}