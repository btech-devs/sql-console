using Btech.Core.Database.Utils;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authentication;
using Btech.Sql.Console.Identity.Authorization;
using Btech.Sql.Console.Identity.Authorization.Configurations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Btech.Sql.Console.Identity;

public static class ServiceCollectionExtensions
{
    private static ServiceDescriptor GetServiceDescriptor(this IServiceCollection serviceCollection, Type serviceType)
    {
        return serviceCollection
            .FirstOrDefault(serviceDescriptor => serviceDescriptor.ServiceType == serviceType);
    }

    private static IServiceCollection ChangePolicyEvaluator(this IServiceCollection serviceCollection)
    {
        ServiceDescriptor policyEvaluator = serviceCollection.GetServiceDescriptor(typeof(IPolicyEvaluator));

        if (policyEvaluator is not null)
        {
            serviceCollection.Remove(policyEvaluator);
        }

        serviceCollection.AddScoped<IPolicyEvaluator, PolicyEvaluator>();

        return serviceCollection;
    }

    private static IamAuthorizationServiceConfiguration BuildAuthorizationServiceConfiguration()
    {
        IamAuthorizationServiceConfiguration config = new IamAuthorizationServiceConfiguration
        {
            GrantedRoles = Environment.GetEnvironmentVariable(Constants.IamServiceGrantedRolesEnvironmentVariableName)
                           ?? Constants.IamServiceGrantedRolesEnvironmentVariableValue
        };

        string iamConfigJson =
            Environment.GetEnvironmentVariable(Constants.IamServiceAccountConfigJsonEnvironmentVariableName);

        if (!iamConfigJson.IsNullOrEmpty())
        {
            // ReSharper disable once PossibleNullReferenceException - checked above
            iamConfigJson = iamConfigJson.Replace("\\n", "\n");

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute - checked above
                IamJsonConfiguration configObject =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<IamJsonConfiguration>(iamConfigJson);

                // ReSharper disable once PossibleNullReferenceException - checked above
                if (!configObject.ClientEmail.IsNullOrEmpty() &&
                    !configObject.PrivateKey.IsNullOrEmpty() &&
                    !configObject.ProjectId.IsNullOrEmpty())
                {
                    config.ServiceAccountEmail = configObject.ClientEmail;
                    config.PrivateKey = configObject.PrivateKey;
                    config.ProjectId = configObject.ProjectId;
                }
            }
            catch (Exception)
            {
                // nothing
            }
        }

        if (config.PrivateKey.IsNullOrEmpty())
        {
            config.PrivateKey =
                EnvironmentUtils.GetRequiredVariable(Constants.IamServiceAccountPrivateKeyEnvironmentVariableName);

            config.ServiceAccountEmail =
                EnvironmentUtils.GetRequiredVariable(Constants.IamServiceAccountEmailEnvironmentVariableName);

            config.ProjectId =
                EnvironmentUtils.GetRequiredVariable(Constants.ProjectIdEnvironmentVariableName);
        }

        return config;
    }

    public static IServiceCollection AddIdentityLayer(this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddConfigurationTransient(BuildAuthorizationServiceConfiguration())
            .AddTransient<IamAuthorizationService>()
            .AddSingleton<IAuthorizationHandler, GoogleIdentityAuthorizationHandler>()
            .AddSingleton<IAuthorizationHandler, SessionAuthorizationHandler>()
            .AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();

        serviceCollection
            .AddAuthentication(
                defaultScheme: Constants.Identity.FullAuthenticationPolicyName)
            .AddScheme<AuthenticationSchemeOptions, GoogleIdentityAuthenticationHandler>(
                authenticationScheme: Constants.Identity.GoogleIdentityAuthenticationSchemeName,
                configureOptions: null)
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(
                authenticationScheme: Constants.Identity.SessionAuthenticationSchemeName,
                configureOptions: null);

        serviceCollection.ChangePolicyEvaluator();

        return serviceCollection;
    }
}