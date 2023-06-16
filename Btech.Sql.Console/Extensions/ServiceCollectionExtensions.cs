using System.Reflection;
using System.Text;
using Btech.Core.Database.Extensions;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.DataStorages.Query;
using Btech.Sql.Console.DataStorages.Session;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Exceptions;
using Btech.Sql.Console.Factories;
using Btech.Sql.Console.Identity;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Providers;
using Btech.Sql.Console.Services;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Database = Btech.Sql.Console.Models.Database;

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
                        .CustomSchemaIds(type => type.GetSchemaId());

                    string filePath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

                    options.IncludeXmlComments(filePath);
                });
    }

    public static IServiceCollection AddFactories(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddSingleton<IConnectorFactory, ConnectorFactory>();

    public static SessionStorageScheme GetSessionStorageScheme()
    {
        SessionStorageScheme scheme;

        bool isStaticSessionStorage =
            !Environment.GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.Host).IsNullOrEmpty() &&
            !Environment.GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.User).IsNullOrEmpty() &&
            !Environment.GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.Password).IsNullOrEmpty();

        if (isStaticSessionStorage)
        {
            string instanceType = Environment
                .GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.InstanceType);

            if (instanceType.IsNullOrEmpty())
                isStaticSessionStorage = false;
            else if (!Enum.TryParse(instanceType, true, out InstanceType _))
                throw new EnvironmentVariableException($"'{Constants.Identity.StaticConnectionEnvironmentVariables.InstanceType}' can not be parsed.");
        }

        if (isStaticSessionStorage)
        {
            scheme = SessionStorageScheme.StaticSessionStorage;
        }
        else if (!Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.Host).IsNullOrEmpty() &&
                 !Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.Name).IsNullOrEmpty() &&
                 !Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.User).IsNullOrEmpty() &&
                 !Environment.GetEnvironmentVariable(Core.Database.Constants.Environment.Database.Password).IsNullOrEmpty())
        {
            scheme = SessionStorageScheme.RemoteDatabase;
        }
        else if (!Environment.GetEnvironmentVariable(Constants.SecretManagerServiceAccountConfigJsonEnvironmentVariableName).IsNullOrEmpty() &&
                 Environment.GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariableName) != Environments.Development)
        {
            scheme = SessionStorageScheme.GoogleCloudSecretManager;
        }
        else if (Environment.GetEnvironmentVariable(Constants.EnvironmentEnvironmentVariableName) == Environments.Development)
        {
            scheme = SessionStorageScheme.LocalDatabase;
        }
        else
        {
            throw new EnvironmentVariableException(
                Constants.EnvironmentEnvironmentVariableName,
                Core.Database.Constants.Environment.Database.Host,
                Core.Database.Constants.Environment.Database.Name,
                Core.Database.Constants.Environment.Database.User,
                Core.Database.Constants.Environment.Database.Password);
        }

        return scheme;
    }

    public static IServiceCollection AddDataStorages(this IServiceCollection serviceCollection)
    {
        switch (GetSessionStorageScheme())
        {
            case SessionStorageScheme.LocalDatabase:
                serviceCollection
                    .AddSingleton<ISessionStorage<SessionData>, LocalSessionStorage>();

                break;

            case SessionStorageScheme.RemoteDatabase:
                serviceCollection
                    .AddEFModel<Database.UserSession>()
                    .AddEFModel<Database.DatabaseSession>()
                    .AddEFModel<Database.SavedQuery>()
                    .AddDatabaseLayer(typeof(Program).Assembly)
                    .AddSingleton<ISessionStorage<SessionData>, DatabaseSessionStorage>()
                    .AddSingleton<ISavedQueryStorage, DatabaseSavedQueryStorage>();

                break;

            case SessionStorageScheme.StaticSessionStorage:
                serviceCollection
                    .AddHttpContextAccessor()
                    .AddSingleton<ISessionStorage<SessionData>, StaticConnectionSessionStorage>();

                break;

            case SessionStorageScheme.GoogleCloudSecretManager:
                serviceCollection
                    .AddScoped<GoogleCloudSecretManagerService>()
                    .AddConfigurationTransient(BuildSecretManagerServiceConfiguration())
                    .AddSingleton<ISessionStorage<SessionData>, GoogleCloudSecretManagerSessionStorage>()
                    .AddSingleton<ISavedQueryStorage, GoogleCloudSecretManagerSavedQueryStorage>();

                break;
        }

        return serviceCollection;
    }

    private static GoogleAccountJsonConfiguration BuildSecretManagerServiceConfiguration()
    {
        string jsonConfiguration =
            Environment.GetEnvironmentVariable(Constants.SecretManagerServiceAccountConfigJsonEnvironmentVariableName);

        GoogleAccountJsonConfiguration config = new GoogleAccountJsonConfiguration();

        if (!jsonConfiguration.IsNullOrEmpty())
        {
            jsonConfiguration = jsonConfiguration?.Replace("\\n", "\n");

            try
            {
                // ReSharper disable once AssignNullToNotNullAttribute - checked above
                GoogleAccountJsonConfiguration configObject =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<GoogleAccountJsonConfiguration>(jsonConfiguration);

                // ReSharper disable once PossibleNullReferenceException - checked above
                if (!configObject.ClientEmail.IsNullOrEmpty() &&
                    !configObject.PrivateKey.IsNullOrEmpty() &&
                    !configObject.ProjectId.IsNullOrEmpty())
                {
                    config.ClientEmail = configObject.ClientEmail;
                    config.PrivateKey = configObject.PrivateKey;
                    config.ProjectId = configObject.ProjectId;
                }
            }
            catch (Exception)
            {
                // nothing
            }
        }

        return config;
    }

    public static IServiceCollection AddConfigurationTransient<TConfiguration>(this IServiceCollection serviceCollection, TConfiguration config)
        where TConfiguration : class, new()
    {
        serviceCollection.AddTransient(_ => config);

        return serviceCollection;
    }
}