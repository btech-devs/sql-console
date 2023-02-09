using Btech.Sql.Console.Identity.Authorization.Configurations;
using Btech.Sql.Console.Utils;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Services;

namespace Btech.Sql.Console.Identity;

public class IamAuthorizationService
{
    public IamAuthorizationService(
        ILogger<IamAuthorizationService> logger, IamAuthorizationServiceConfiguration config)
    {
        this.Logger = logger;
        this.Config = config;
        this.Service = this.InitializeService();
        this.GrantedRoles = this.InitializeGrantedRoles();
    }

    private ILogger Logger { get; }

    private IamAuthorizationServiceConfiguration Config { get; }

    private CloudResourceManagerService Service { get; }

    private List<string> GrantedRoles { get; }

    private string PreparePrivateKey()
    {
        return this.Config.PrivateKey.Replace("\\n", "\n");
    }

    private CloudResourceManagerService InitializeService()
    {
        ServiceAccountCredential.Initializer initializer =
            new ServiceAccountCredential.Initializer(this.Config.ServiceAccountEmail)
            {
                ProjectId = this.Config.ProjectId
            };

        CloudResourceManagerService res = new CloudResourceManagerService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = GoogleCredential
                    .FromServiceAccountCredential(
                        new ServiceAccountCredential(
                            initializer.FromPrivateKey(this.PreparePrivateKey())))
                    .CreateScoped(CloudResourceManagerService.Scope.CloudPlatformReadOnly)
            });

        this.Logger.LogInformation($"IAM service successfully initialized with account: '{this.Config.ServiceAccountEmail}'.");

        return res;
    }

    private List<string> InitializeGrantedRoles()
    {
        return this.Config.GrantedRoles
            .Split(
                separator: ',',
                options: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(role => $"roles/{role}")
            .ToList();
    }

    private async Task<(bool Succeeded, Policy Policy)> TryGetIamPolicyAsync()
    {
        Policy policy = null;
        bool succeeded = false;

        try
        {
            policy = await this.Service
                .Projects
                .GetIamPolicy(
                    body: new GetIamPolicyRequest(),
                    resource: this.Config.ProjectId)
                .ExecuteAsync();

            succeeded = true;
            this.Logger.LogInformation("IAM policy received successfully.");
        }
        catch (GoogleApiException googleApiException)
        {
            this.Logger?.LogCritical(googleApiException, $"'GetIamPolicy' request failed. GoogleApiException occured: '{googleApiException.Message}'.");

            await AuditNotifier.ReportExceptionAsync(googleApiException, $"{nameof(IamAuthorizationService)}.{nameof(this.TryGetIamPolicyAsync)}");
        }
        catch (Exception exception)
        {
            this.Logger?.LogCritical(exception, $"'GetIamPolicy' request failed. Message: '{exception.Message}'.");

            await AuditNotifier.ReportExceptionAsync(exception, $"{nameof(IamAuthorizationService)}.{nameof(this.TryGetIamPolicyAsync)}");
        }

        if (succeeded && policy is null)
        {
            this.Logger?.LogCritical("'IAM policy' is null.");
            succeeded = false;
        }

        return (succeeded, policy);
    }

    public async Task<(bool Succeeded, bool Allowed)> IsAllowedUserAsync(string userEmail)
    {
        bool isAllowed = false;

        (bool Succeeded, Policy Policy) policyResponse = await this.TryGetIamPolicyAsync();

        if (policyResponse.Succeeded)
        {
            List<Binding> policyGrantedRoles = policyResponse.Policy.Bindings
                .Where(binding => this.GrantedRoles.Contains(binding.Role))
                .ToList();

            if (policyGrantedRoles.Any())
            {
                isAllowed = policyGrantedRoles
                    .Any(role => role.Members.Contains($"user:{userEmail}"));
            }
            else
            {
                this.Logger?.LogError($"No granted roles found in project '{this.Config.ProjectId}' IAM policy. Check projects environment variable '{Constants.IamServiceGrantedRolesEnvironmentVariableName}' or configure project on the GCP.");
            }
        }

        return (policyResponse.Succeeded, isAllowed);
    }
}