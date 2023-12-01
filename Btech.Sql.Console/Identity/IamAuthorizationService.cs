using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Exceptions;
using Btech.Sql.Console.Identity.Authorization.Configurations;
using Btech.Sql.Console.Utils;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Google.Apis.Services;

namespace Btech.Sql.Console.Identity;

/// <summary>
/// Provides functionality to authorize IAM policies.
/// </summary>
public class IamAuthorizationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IamAuthorizationService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="config">The IAM authorization service configuration.</param>
    public IamAuthorizationService(ILogger<IamAuthorizationService> logger, IamAuthorizationServiceConfiguration config)
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

    private CloudResourceManagerService InitializeService()
    {
        CloudResourceManagerService cloudResourceManagerService;

        ServiceAccountCredential.Initializer initializer =
            new ServiceAccountCredential.Initializer(this.Config.ServiceAccountEmail)
            {
                ProjectId = this.Config.ProjectId
            };

        try
        {
            initializer = initializer.FromPrivateKey(this.Config.PrivateKey.Replace("\\n", "\n"));
        }
        catch (Exception exception)
        {
            string environmentVariableName = $"{Constants.IamServiceAccountConfigJsonEnvironmentVariableName}.private_key";

            if (Environment.GetEnvironmentVariable(Constants.IamServiceAccountConfigJsonEnvironmentVariableName)
                is null)
            {
                environmentVariableName = Constants.IamServiceAccountPrivateKeyEnvironmentVariableName;
            }

            throw new EnvironmentVariableException(exception, environmentVariableName);
        }

        try
        {
            cloudResourceManagerService = new CloudResourceManagerService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential
                        .FromServiceAccountCredential(
                            new ServiceAccountCredential(initializer))
                        .CreateScoped(CloudResourceManagerService.Scope.CloudPlatformReadOnly)
                });
        }
        catch (Exception exception)
        {
            throw new EnvironmentVariableException(exception, Constants.IamServiceAccountConfigJsonEnvironmentVariableName);
        }

        this.Logger.LogInformation($"IAM service successfully initialized with account: '{this.Config.ServiceAccountEmail}'.");

        return cloudResourceManagerService;
    }

    private List<string> InitializeGrantedRoles()
    {
        string invalidRole;

        List<string> roles =
            this.Config.GrantedRoles
                .Split(
                    separator: ',',
                    options: StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        if ((invalidRole = roles.FirstOrDefault(
                role =>
                    role != Constants.Identity.IamServiceRoleNames.CloudSqlAdmin &&
                    role != Constants.Identity.IamServiceRoleNames.CloudSqlClient &&
                    role != Constants.Identity.IamServiceRoleNames.CloudSqlEditor &&
                    role != Constants.Identity.IamServiceRoleNames.Owner &&
                    role != Constants.Identity.IamServiceRoleNames.Editor)) is not null)
        {
            throw new ApplicationException($"Invalid configuration '{Constants.IamServiceGrantedRolesEnvironmentVariableName}': role '{invalidRole}' is not allowed.");
        }

        return roles;
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

            throw new ExternalServiceException(googleApiException, "GoogleAPI", googleApiException.Message);
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

    private UserRole GetUserRole(List<Binding> userRoles)
    {
        UserRole role = UserRole.None;

        if (userRoles.Any(
                binding =>
                    binding.Role is
                        Constants.Identity.IamServiceRoleNames.CloudSqlAdmin or
                        Constants.Identity.IamServiceRoleNames.Owner))
        {
            role = UserRole.Admin;
        }
        else if (userRoles.Any(
                     binding => binding.Role is
                         Constants.Identity.IamServiceRoleNames.CloudSqlEditor or
                         Constants.Identity.IamServiceRoleNames.Editor))
        {
            role = UserRole.Editor;
        }
        else if (userRoles.Any(binding => binding.Role == Constants.Identity.IamServiceRoleNames.CloudSqlClient))
        {
            role = UserRole.Client;
        }

        return role;
    }

    /// <summary>
    /// Determines whether the specified user is allowed access to the Cloud SQL instance.
    /// </summary>
    /// <param name="userEmail">The email address of the user to check.</param>
    /// <returns>A tuple containing a flag indicating whether the user is allowed, a flag indicating whether the check succeeded, and the user's role if the check succeeded.</returns>
    public async Task<(bool Succeeded, bool Allowed, UserRole role)> IsAllowedUserAsync(string userEmail)
    {
        bool isAllowed = false;
        UserRole userRole = UserRole.None;

        (bool Succeeded, Policy Policy) policyResponse = await this.TryGetIamPolicyAsync();

        if (policyResponse.Succeeded)
        {
            List<Binding> policyGrantedRoles = policyResponse.Policy.Bindings
                .Where(binding => this.GrantedRoles.Contains(binding.Role))
                .ToList();

            if (policyGrantedRoles.Any())
            {
                List<Binding> userRoles = policyGrantedRoles
                    .Where(role => role.Members.Any(member => member.ToLower() == $"user:{userEmail.ToLower()}"))
                    .ToList();

                isAllowed = userRoles.Any();

                if (isAllowed)
                {
                    userRole = this.GetUserRole(userRoles);
                }
            }
            else
            {
                this.Logger?.LogError($"No granted roles found in project '{this.Config.ProjectId}' IAM policy. Configure project on the GCP.");
            }
        }

        return (policyResponse.Succeeded, isAllowed, userRole);
    }
}