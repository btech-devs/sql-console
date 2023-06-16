using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Services;
using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;

namespace Btech.Sql.Console.DataStorages.Query;

/// <summary>
/// Represents a saved query storage implementation that uses a Google Cloud Secret Manager.
/// </summary>
public sealed class GoogleCloudSecretManagerSavedQueryStorage : ISavedQueryStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCloudSecretManagerSavedQueryStorage"/> class with the specified logger and Google Cloud Secret Manager service.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging messages.</param>
    /// <param name="service">The Google Cloud Secret Manager service instance to use for retrieving and storing secrets.</param>
    public GoogleCloudSecretManagerSavedQueryStorage(
        ILogger<GoogleCloudSecretManagerSavedQueryStorage> logger,
        GoogleCloudSecretManagerService service)
    {
        this.SecretManagerService = service;
        this.Logger = logger;
    }

    private ILogger Logger { get; }
    private GoogleCloudSecretManagerService SecretManagerService { get; }

    private string GetSecretName(string email) =>
        $"{email}-saved-queries".GetMd5();

    private async Task<bool> SaveAsync(string email, List<QueryData> data)
    {
        data = data
            .OrderByDescending(query => query.Id.HasValue)
            .ThenBy(query => query.Id)
            .ToList();

        for (var index = 0; index < data.Count; index++)
            data[index].Id ??= index + 1;

        SecretVersion secretVersion = null;

        string secretName = this.GetSecretName(email);

        AccessSecretVersionResponse secretVersionSessionData =
            await this.SecretManagerService.GetSecretVersion(this.GetSecretName(email), Constants.Identity.SecretManagerSessionDataVersionId);

        if (secretVersionSessionData is not null && secretVersionSessionData.Payload?.Data is not null)
        {
            await this.DeleteAsync(email);
        }

        Secret secret = await this.SecretManagerService.CreateSecretAsync(secretName);

        if (secret is not null)
        {
            secretVersion =
                await this.SecretManagerService.AddSecretVersion(this.GetSecretName(email), data.JsonSerialize());
        }

        if (secretVersion is null ||
            secretVersion.State != SecretVersion.Types.State.Enabled ||
            !secretVersion.Name.EndsWith($"/{Constants.Identity.SecretManagerSessionDataVersionId}"))
        {
            string details = secretVersion is null
                ? "'SecretVersion' was not created"
                : secretVersion.State is not SecretVersion.Types.State.Enabled
                    ? "'SecretVersion.State' is not 'Enabled'"
                    : !secretVersion.Name.EndsWith($"/{Constants.Identity.SecretManagerSessionDataVersionId}")
                        ? $"'SecretVersion.Id' is not '{Constants.Identity.SecretManagerSessionDataVersionId}'"
                        : "Unexpected error";

            this.Logger.LogError($"Error on creating secret version. {details}.");

            await this.DeleteAsync(email);

            throw new ApplicationException("Something went wrong while creating the session, try again or contact the administrator.");
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SaveAsync(string email, QueryData data)
    {
        List<QueryData> queryDataList = await this.GetAsync(email);

        queryDataList.Add(data);

        return await this.SaveAsync(email, queryDataList);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(string email, QueryData data)
    {
        bool result;

        if (data.Id.HasValue)
        {
            List<QueryData> queryDataList = await this.GetAsync(email);

            queryDataList = queryDataList == null
                ? new List<QueryData>()
                : queryDataList.Where(query => query.Id != data.Id).ToList();

            queryDataList.Add(data);

            result = await this.SaveAsync(email, queryDataList);
        }
        else
        {
            result = await this.SaveAsync(email, data);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string email, long? id = null)
    {
        bool result;

        if (id == null)
        {
            await this.SecretManagerService
                .DeleteSecretAsync(this.GetSecretName(email));

            result = true;
        }
        else
        {
            List<QueryData> queryDataList = await this.GetAsync(email);

            queryDataList = queryDataList
                .Where(queryData => queryData.Id != id)
                .ToList();

            result = await this.SaveAsync(email, queryDataList);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<QueryData>> GetAsync(string email)
    {
        List<QueryData> deserialized = null;
        string savedQueryJson = null;

        AccessSecretVersionResponse secretVersionSessionData =
            await this.SecretManagerService.GetSecretVersion(this.GetSecretName(email), Constants.Identity.SecretManagerSessionDataVersionId);

        if (secretVersionSessionData is not null && secretVersionSessionData.Payload?.Data is not null)
        {
            savedQueryJson = secretVersionSessionData.Payload.Data.ToStringUtf8();
        }

        if (savedQueryJson is not null)
        {
            deserialized = JsonConvert.DeserializeObject<List<QueryData>>(savedQueryJson);
        }

        return deserialized ?? new List<QueryData>();
    }

    /// <inheritdoc />
    public async Task<QueryData> GetAsync(string email, long id) =>
        (await this.GetAsync(email)).FirstOrDefault(query => query.Id == id);
}