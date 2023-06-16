using Btech.Sql.Console.Base;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Entities;
using Btech.Sql.Console.Models.Responses.Base;
using Microsoft.AspNetCore.Mvc;
using ServiceCollectionExtensions = Btech.Sql.Console.Extensions.ServiceCollectionExtensions;

namespace Btech.Sql.Console.Controllers;

/// <summary>
/// Controller to handle saved queries.
/// </summary>
[Controller]
[Route("api/saved-queries")]
public class SavedQueryController : SessionRelatedControllerBase
{
    public SavedQueryController(ILogger<SavedQueryController> logger, ISavedQueryStorage savedQueryStorage = null) : base(logger)
    {
        this.SavedQueryStorage = savedQueryStorage;
    }

    private ISavedQueryStorage SavedQueryStorage { get; }

    /// <summary>
    /// Gets all saved queries.
    /// </summary>
    /// <param name="includeQuery">Whether to include the SQL query in the response.</param>
    /// <returns>A list of all saved queries.</returns>
    [HttpGet]
    public async Task<Response<List<SavedQuery>>> GetAllAsync([FromQuery] bool includeQuery = false)
    {
        Response<List<SavedQuery>> response = new();

        if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.GoogleCloudSecretManager ||
            ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase)
        {
            List<QueryData> queryDataList = await this.SavedQueryStorage
                .GetAsync(this.GetUserClaim(Constants.Identity.ClaimTypes.Email));

            response.Data = queryDataList.Select(
                    queryData => new SavedQuery
                    {
                        Id = queryData.Id,
                        Name = queryData.Name,
                        Query = includeQuery ? queryData.Query : null
                    })
                .OrderBy(query => query.Id)
                .ToList();
        }
        else
        {
            this.Response.StatusCode = 501;
            response.ErrorMessage = "The feature is not supported by the configuration.";
        }

        return response;
    }

    /// <summary>
    /// Gets a single saved query by its ID.
    /// </summary>
    /// <param name="id">The ID of the saved query to retrieve.</param>
    /// <returns>The saved query with the specified ID.</returns>
    [HttpGet("{id}")]
    public async Task<Response<SavedQuery>> GetAsync([FromRoute] long id)
    {
        Response<SavedQuery> response = new();

        if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.GoogleCloudSecretManager ||
            ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase)
        {
            QueryData queryData = await this.SavedQueryStorage
                .GetAsync(this.GetUserClaim(Constants.Identity.ClaimTypes.Email), id);

            response.Data = new SavedQuery
            {
                Id = queryData.Id,
                Name = queryData.Name,
                Query = queryData.Query
            };
        }
        else
        {
            this.Response.StatusCode = 501;
            response.ErrorMessage = "The feature is not supported by the configuration.";
        }

        return response;
    }

    /// <summary>
    /// Saves a new query.
    /// </summary>
    /// <param name="savedQuery">The query to save.</param>
    /// <returns>A response indicating whether the query was successfully saved.</returns>
    [HttpPost]
    public async Task<Response> PostAsync([FromBody] SavedQuery savedQuery)
    {
        const int maxSaveCount = 20;

        Response response = new();

        if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.GoogleCloudSecretManager ||
            ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase)
        {
            List<QueryData> queryDataList = await this.SavedQueryStorage
                .GetAsync(this.GetUserClaim(Constants.Identity.ClaimTypes.Email));

            if (queryDataList.Count < maxSaveCount)
            {
                await this.SavedQueryStorage.SaveAsync(
                    this.GetUserClaim(Constants.Identity.ClaimTypes.Email),
                    new QueryData
                    {
                        Name = savedQuery.Name,
                        Query = savedQuery.Query
                    });
            }
            else
            {
                response.ErrorMessage = $"Query not saved. Cause: The maximum number of saved queries has already been reached. Total count: {queryDataList.Count}.";
            }
        }
        else
        {
            this.Response.StatusCode = 501;
            response.ErrorMessage = "The feature is not supported by the configuration.";
        }

        return response;
    }

    /// <summary>
    /// Update a query by <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of query.</param>
    /// <param name="savedQuery">The query to save.</param>
    /// <returns>A response indicating whether the query was successfully updated.</returns>
    [HttpPut("{id}")]
    public async Task<Response> PutAsync([FromRoute] long id, [FromBody] SavedQuery savedQuery)
    {
        Response response = new();

        if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.GoogleCloudSecretManager ||
            ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase)
        {
            await this.SavedQueryStorage.UpdateAsync(
                this.GetUserClaim(Constants.Identity.ClaimTypes.Email),
                new QueryData
                {
                    Id = id,
                    Name = savedQuery.Name,
                    Query = savedQuery.Query
                });
        }
        else
        {
            this.Response.StatusCode = 501;
            response.ErrorMessage = "The feature is not supported by the configuration.";
        }

        return response;
    }

    /// <summary>
    /// Delete a query by <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of query.</param>
    /// <returns>A response indicating whether the query was successfully deleted.</returns>
    [HttpDelete("{id}")]
    public async Task<Response> DeleteAsync([FromRoute] long id)
    {
        Response response = new();

        if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.GoogleCloudSecretManager ||
            ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase)
        {
            await this.SavedQueryStorage.DeleteAsync(this.GetUserClaim(Constants.Identity.ClaimTypes.Email), id);
        }
        else
        {
            this.Response.StatusCode = 501;
            response.ErrorMessage = "The feature is not supported by the configuration.";
        }

        return response;
    }
}