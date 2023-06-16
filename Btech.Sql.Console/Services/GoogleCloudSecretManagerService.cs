using Btech.Core.Database.Utils;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Exceptions;
using Btech.Sql.Console.Utils;
using Google;
using Google.Cloud.SecretManager.V1;
using Google.Protobuf;
using Google.Api.Gax.ResourceNames;
using Grpc.Core;

namespace Btech.Sql.Console.Services;

/// <summary>
/// Service for interacting with Google Cloud Secret Manager.
/// </summary>
public class GoogleCloudSecretManagerService
{
    /// <summary>
    /// Creates a new instance of the <see cref="GoogleCloudSecretManagerService"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="config">The configuration to use for the service.</param>
    public GoogleCloudSecretManagerService(
        ILogger<GoogleCloudSecretManagerService> logger, GoogleAccountJsonConfiguration config)
    {
        this.Config = config;
        this.Logger = logger;

        this.Client = this.InitializeService();
    }

    private ILogger Logger { get; }
    private GoogleAccountJsonConfiguration Config { get; }
    private SecretManagerServiceClient Client { get; }

    private SecretManagerServiceClient InitializeService()
    {
        SecretManagerServiceClientBuilder clientBuilder = new SecretManagerServiceClientBuilder
        {
            JsonCredentials = EnvironmentUtils
                .GetRequiredVariable(Constants.SecretManagerServiceAccountConfigJsonEnvironmentVariableName)
        };

        SecretManagerServiceClient client = clientBuilder.Build();

        this.Logger.LogInformation($"SecretManager service successfully initialized with account: '{this.Config.ClientEmail}'.");

        return client;
    }

    /// <summary>
    /// Creates a new secret with the specified name.
    /// </summary>
    /// <param name="secretName">The name of the secret to create.</param>
    /// <returns>The newly created secret.</returns>
    public async Task<Secret> CreateSecretAsync(string secretName)
    {
        Secret secret;

        try
        {
            secret = await this.Client.CreateSecretAsync(
                new CreateSecretRequest
                {
                    ParentAsProjectName = new ProjectName(this.Config.ProjectId),
                    SecretId = secretName,
                    Secret = new Secret
                    {
                        Replication = new Replication
                        {
                            Automatic = new Replication.Types.Automatic()
                        }
                    }
                });
        }
        catch (RpcException rpcException)
        {
            await AuditNotifier.ReportExceptionAsync(
                rpcException, $"{this.GetType().Name}.{nameof(this.CreateSecretAsync)}");

            throw new ExternalServiceException(rpcException, this.GetType().Name, rpcException.Message);
        }
        catch (GoogleApiException googleApiException)
        {
            await AuditNotifier.ReportExceptionAsync(
                googleApiException, $"{this.GetType().Name}.{nameof(this.CreateSecretAsync)}");

            throw new ExternalServiceException(googleApiException, this.GetType().Name, googleApiException.Message);
        }

        return secret;
    }

    /// <summary>
    /// Adds a new version to the specified secret.
    /// </summary>
    /// <param name="secretName">The name of the secret to add the version to.</param>
    /// <param name="payloadData">The payload data for the new version.</param>
    /// <returns>The newly created secret version.</returns>
    public async Task<SecretVersion> AddSecretVersion(string secretName, string payloadData)
    {
        SecretVersion secretVersion;

        try
        {
            secretVersion = await this.Client
                .AddSecretVersionAsync(
                    new AddSecretVersionRequest
                    {
                        ParentAsSecretName = new SecretName(this.Config.ProjectId, secretName),
                        Payload = new SecretPayload
                        {
                            Data = ByteString.CopyFromUtf8(payloadData)
                        }
                    });
        }
        catch (RpcException rpcException)
        {
            await AuditNotifier.ReportExceptionAsync(
                rpcException, $"{this.GetType().Name}.{nameof(this.AddSecretVersion)}");

            throw new ExternalServiceException(rpcException, this.GetType().Name, rpcException.Message);
        }
        catch (GoogleApiException googleApiException)
        {
            await AuditNotifier.ReportExceptionAsync(
                googleApiException, $"{this.GetType().Name}.{nameof(this.AddSecretVersion)}");

            throw new ExternalServiceException(googleApiException, this.GetType().Name, googleApiException.Message);
        }

        return secretVersion;
    }

    /// <summary>
    /// Deletes the specified secret.
    /// </summary>
    /// <param name="secretName">The name of the secret to delete.</param>
    public async Task DeleteSecretAsync(string secretName)
    {
        try
        {
            await this.Client
                .DeleteSecretAsync(
                    new DeleteSecretRequest
                    {
                        SecretName = new SecretName(this.Config.ProjectId, secretName)
                    });
        }
        catch (RpcException rpcException)
        {
            if (rpcException.Status.StatusCode != StatusCode.NotFound)
            {
                await AuditNotifier.ReportExceptionAsync(rpcException, $"{this.GetType().Name}.{nameof(this.DeleteSecretAsync)}");

                throw new ExternalServiceException(rpcException, this.GetType().Name, rpcException.Message);
            }
        }
        catch (GoogleApiException googleApiException)
        {
            await AuditNotifier.ReportExceptionAsync(
                googleApiException, $"{this.GetType().Name}.{nameof(this.DeleteSecretAsync)}");

            throw new ExternalServiceException(googleApiException, this.GetType().Name, googleApiException.Message);
        }
    }

    /// <summary>
    /// Gets the specified version of the specified secret.
    /// </summary>
    /// <param name="secretName">The name of the secret to get the version of.</param>
    /// <param name="secretVersionId">The ID of the version to get.</param>
    /// <returns>The specified secret version.</returns>
    public async Task<AccessSecretVersionResponse> GetSecretVersion(string secretName, string secretVersionId)
    {
        AccessSecretVersionResponse secretVersion = null;

        SecretVersionName secretVersionName =
            new SecretVersionName(this.Config.ProjectId, secretName, secretVersionId);

        try
        {
            secretVersion = await this.Client.AccessSecretVersionAsync(secretVersionName);
        }
        catch (RpcException rpcException)
        {
            if (rpcException.Status.StatusCode != StatusCode.NotFound)
            {
                await AuditNotifier.ReportExceptionAsync(rpcException, $"{this.GetType().Name}.{nameof(this.GetSecretVersion)}");

                throw new ExternalServiceException(rpcException, this.GetType().Name, rpcException.Message);
            }
        }
        catch (GoogleApiException googleApiException)
        {
            await AuditNotifier.ReportExceptionAsync(
                googleApiException, $"{this.GetType().Name}.{nameof(this.GetSecretVersion)}");

            throw new ExternalServiceException(googleApiException, this.GetType().Name, googleApiException.Message);
        }

        return secretVersion;
    }
}