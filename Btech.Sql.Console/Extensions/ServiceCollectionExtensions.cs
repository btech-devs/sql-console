using System.Text;
using Btech.Core.Database.Extensions;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.DataStorages;
using Btech.Sql.Console.Factories;
using Btech.Sql.Console.Identity;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Database;
using Btech.Sql.Console.Providers;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;

namespace Btech.Sql.Console.Extensions;

public static class ServiceCollectionExtensions
{
    private static string GetSchemaId(this Type type)
    {
        StringBuilder schemaId = new();

        if (!type.Namespace!.IsNullOrEmpty())
        {
            if (type.Namespace!.StartsWith("Btech.Sql.Console."))
                schemaId
                    .Append(type.Namespace.Replace("Btech.Sql.Console.", string.Empty))
                    .Append(".");
        }

        if (type.Name.Contains("`"))
            schemaId.Append(type.Name.Substring(0, type.Name.LastIndexOf("`", StringComparison.Ordinal)));
        else
            schemaId.Append(type.Name);

        if (type.IsGenericType)
        {
            schemaId.Append("<");

            bool isFirst = true;

            foreach (Type genericType in type.GenericTypeArguments)
            {
                if (!isFirst)
                    schemaId.Append(",");

                schemaId.Append(GetSchemaId(genericType));
                isFirst = false;
            }

            schemaId.Append(">");
        }

        return schemaId.ToString();
    }

    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddAPILayer(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection
            .AddControllers()
            .AddNewtonsoftJson(
                options => options.SerializerSettings.Converters.Add(new StringEnumConverter()));

        serviceCollection
            .AddIdentityLayer()
            .AddConfigurations(configuration)
            .AddFactories()
            .AddProviders()
            .AddDataStorages();

        return serviceCollection
            // swagger
            .AddSwaggerGen(
                options =>
                {
                    options
                        .SwaggerDoc(
                            name: "v1",
                            info: new OpenApiInfo { Title = "btech-sql-console", Version = "v1" });

                    options
                        .TagActionsBy(
                            tagsSelector: endpoint =>
                            {
                                List<string> result = new();

                                string[] relativePathParts = endpoint.RelativePath?.Split("/");

                                if (relativePathParts?.Length is >= 1 and <= 2)
                                    result.Add(endpoint.RelativePath?.Split("/")[0]);
                                else if (relativePathParts?.Length is >= 2 and <= 3)
                                    result.Add($"{endpoint.RelativePath?.Split("/")[0]}/{endpoint.RelativePath?.Split("/")[1]}");
                                else if (relativePathParts?.Length is >= 4 and <= 5)
                                    result.Add($"{endpoint.RelativePath?.Split("/")[0]}/{endpoint.RelativePath?.Split("/")[1]}/{endpoint.RelativePath?.Split("/")[3]}");
                                else if (relativePathParts?.Length is >= 6 and <= 7)
                                    result.Add($"{endpoint.RelativePath?.Split("/")[0]}/{endpoint.RelativePath?.Split("/")[1]}/{endpoint.RelativePath?.Split("/")[3]}/{endpoint.RelativePath?.Split("/")[5]}");

                                return result;
                            });

                    options
                        .CustomSchemaIds(type => type.GetSchemaId());
                });
    }

    public static IServiceCollection AddFactories(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IConnectorFactory, ConnectorFactory>();

    public static IServiceCollection AddDataStorages(this IServiceCollection serviceCollection)
    {
        if (!Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.Host).IsNullOrEmpty() &&
            !Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.Name).IsNullOrEmpty() &&
            !Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.User).IsNullOrEmpty() &&
            !Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.Password).IsNullOrEmpty() ||
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Production ||
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Staging)
            serviceCollection
                .AddEFModel<UserSession>()
                .AddEFModel<DatabaseSession>()
                .AddDatabaseLayer(typeof(Program).Assembly)
                .AddSingleton<ISessionStorage<SessionData>, DatabaseSessionStorage>();
        else
            serviceCollection
                .AddSingleton<ISessionStorage<SessionData>, LocalSessionStorage>();

        return serviceCollection;
    }

    public static IServiceCollection AddConfigurationTransient<TConfiguration>(
        this IServiceCollection serviceCollection, TConfiguration config)
        where TConfiguration : class, new()
    {
        serviceCollection.AddTransient(_ => config);

        return serviceCollection;
    }
}