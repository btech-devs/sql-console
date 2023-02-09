using Btech.Core.Database.Utils;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Extensions;

namespace Btech.Sql.Console.Providers;

public static class ServiceCollectionExtensions
{
    private static CryptographyConfiguration BuildCryptographyConfiguration()
    {
        CryptographyConfiguration config = new CryptographyConfiguration
        {
            PrivateKey = EnvironmentUtils.GetRequiredVariable(Constants.CryptographyPrivateKeyEnvironmentVariableName),
            PublicKey = EnvironmentUtils.GetRequiredVariable(Constants.CryptographyPublicKeyEnvironmentVariableName)
        };

        return config;
    }

    public static IServiceCollection AddProviders(
        this IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddScoped<JwtProvider>()
            .AddScoped<CryptographyProvider>()
            .AddConfigurationTransient(BuildCryptographyConfiguration());

        return serviceCollection;
    }
}