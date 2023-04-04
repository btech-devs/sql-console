using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Services;
using Google.Cloud.SecretManager.V1;
using Newtonsoft.Json;

namespace Btech.Sql.Console.DataStorages;

public sealed class GoogleCloudSecretManagerSessionStorage : ISessionStorage<SessionData>
{
    public GoogleCloudSecretManagerSessionStorage(
        ILogger<GoogleCloudSecretManagerSessionStorage> logger,
        GoogleCloudSecretManagerService service)
    {
        this.SecretManagerService = service;
        this.Logger = logger;
    }

    private ILogger Logger { get; }
    private GoogleCloudSecretManagerService SecretManagerService { get; }

    public async Task<bool> SaveAsync(string email, SessionData data)
    {
        SecretVersion secretVersion = null;

        Secret secret = await this.SecretManagerService.CreateSecretAsync(email.GetMd5());

        if (secret is not null)
        {
            secretVersion =
                await this.SecretManagerService.AddSecretVersion(email.GetMd5(), data.JsonSerialize());
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

    public async Task<bool> UpdateAsync(string email, SessionData updatedSessionData)
    {
        bool result = await this.DeleteAsync(email);

        if (result)
        {
            result = await this.SaveAsync(email, updatedSessionData);
        }

        return result;
    }

    public async Task<bool> DeleteAsync(string email)
    {
        await this.SecretManagerService
            .DeleteSecretAsync(email.GetMd5());

        return true;
    }

    public async Task<SessionData> GetAsync(string email)
    {
        SessionData deserialized = null;
        string sessionDataJson = null;

        AccessSecretVersionResponse secretVersionSessionData =
            await this.SecretManagerService.GetSecretVersion(email.GetMd5(), Constants.Identity.SecretManagerSessionDataVersionId);

        if (secretVersionSessionData is not null && secretVersionSessionData.Payload?.Data is not null)
        {
            sessionDataJson = secretVersionSessionData.Payload.Data.ToStringUtf8();
        }

        if (sessionDataJson is not null)
        {
            deserialized = JsonConvert.DeserializeObject<SessionData>(sessionDataJson);
        }

        return deserialized;
    }
}