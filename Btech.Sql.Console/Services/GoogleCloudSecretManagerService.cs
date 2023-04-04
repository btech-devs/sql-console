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

public class GoogleCloudSecretManagerService
{
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