using Btech.Core.Database.Utils;
using Btech.Sql.Console.Extensions;

namespace Btech.Sql.Console.Configurations;

public static class ServiceCollectionExtensions
{
    private static GoogleProjectConfiguration BuildGoogleProjectConfiguration()
    {
        GoogleProjectConfiguration config = new GoogleProjectConfiguration
        {
            ClientId = EnvironmentUtils.GetRequiredVariable(Constants.ClientIdEnvironmentVariableName),
            ClientSecret = EnvironmentUtils.GetRequiredVariable(Constants.ClientSecretEnvironmentVariableName)
        };

        return config;
    }

    public static IServiceCollection AddConfigurations(
        this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddConfiguration<JwtConfiguration>(configuration)
            .AddConfigurationTransient(BuildGoogleProjectConfiguration());

        return serviceCollection;
    }

    public static IServiceCollection AddConfiguration<TConfiguration>(
        this IServiceCollection serviceCollection, IConfiguration configuration, string name = null)
        where TConfiguration : class, new()
    {
        TConfiguration config = new();

        configuration.GetSection(name ?? typeof(TConfiguration).Name).Bind(config);

        serviceCollection.AddTransient(_ => config);

        return serviceCollection;
    }
}