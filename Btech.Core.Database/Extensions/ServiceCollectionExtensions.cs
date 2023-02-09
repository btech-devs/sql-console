using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Btech.Core.Database.Base;
using Btech.Core.Database.Configurations;
using Btech.Core.Database.Interfaces;
using Btech.Core.Database.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Btech.Core.Database.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMigrationAssembly(this IServiceCollection serviceCollection, Assembly migrationAssembly)
    {
        return serviceCollection.AddSingleton(typeof(Assembly), migrationAssembly);
    }

    public static IServiceCollection AddConfiguration<TConfiguration>(this IServiceCollection serviceCollection, IConfiguration configuration, string name = null)
        where TConfiguration : class, new()
    {
        serviceCollection
            .Configure<TConfiguration>(configuration.GetSection(name ?? typeof(TConfiguration).Name))
            .AddSingleton(sp => sp.GetRequiredService<IOptions<TConfiguration>>().Value);

        return serviceCollection;
    }

    public static IServiceCollection AddTransientIfNotAdded
        <TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection serviceCollection)
        where TService : class
        where TImplementation : class, TService
    {
        if (serviceCollection.All(service => service.ServiceType != typeof(TService)))
            serviceCollection
                .AddTransient<TService, TImplementation>();

        return serviceCollection;
    }

    /// <summary>
    /// Adds database configurations.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public static IServiceCollection AddDatabaseLayer(this IServiceCollection serviceCollection, Assembly assembly, IConfiguration configuration = null)
    {
        serviceCollection
            .AddMigrationAssembly(assembly)
            .AddDatabaseConfiguration(configuration)
            .AddDbContext<DbContext, PgSqlDbContext>(
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Transient)
            .AddTransientIfNotAdded<IUnitOfWork, UnitOfWork<DbContext>>()
            .AddTransientIfNotAdded<IUnitOfWorkFactory, UnitOfWorkFactory>()
            .AddTransient<IList<EntityBase>>(serviceProvider => serviceProvider.GetServices<EntityBase>().ToList())
            .AddTransient<IList<Assembly>>(serviceProvider => serviceProvider.GetServices<Assembly>().ToList());

        return serviceCollection;
    }

    public static IServiceCollection AddDatabaseConfiguration(
        this IServiceCollection serviceCollection, IConfiguration configuration = null)
    {
        if (configuration != null)
            serviceCollection
                .AddConfiguration<DatabaseConfiguration>(configuration);
        else
        {
            bool pooling = Environment
                .GetEnvironmentVariable(Constants.Environment.Database.Pooling).ToNullableBool() ?? true;

            int maxPoolSize = Environment
                .GetEnvironmentVariable(Constants.Environment.Database.MaxPoolSize).ToNullableInt() ?? 50;

            int commandTimeout = Environment
                .GetEnvironmentVariable(Constants.Environment.Database.CommandTimeout).ToNullableInt() ?? 600;

            int maxBatchSize = Environment
                .GetEnvironmentVariable(Constants.Environment.Database.MaxBatchSize).ToNullableInt() ?? 1000;

            bool ssl = Environment
                .GetEnvironmentVariable(Constants.Environment.Database.Ssl).ToNullableBool() ?? false;

            serviceCollection
                .AddSingleton(
                    _ => new DatabaseConfiguration
                    {
                        Database = EnvironmentUtils.GetRequiredVariable(Constants.Environment.Database.Name),
                        Host = EnvironmentUtils.GetRequiredVariable(Constants.Environment.Database.Host),
                        Username = EnvironmentUtils.GetRequiredVariable(Constants.Environment.Database.User),
                        Password = EnvironmentUtils.GetRequiredVariable(Constants.Environment.Database.Password),
                        Pooling = pooling,
                        MaxPoolSize = maxPoolSize,
                        CommandTimeout = commandTimeout,
                        MaxBatchSize = maxBatchSize,
                        Ssl = ssl
                    });
        }

        return serviceCollection;
    }

    /// <summary>
    /// Adds an Entity Framework model.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddEFModel<TEntity>(this IServiceCollection serviceCollection)
        where TEntity : EntityBase
    {
        serviceCollection
            .AddSingleton<EntityBase, TEntity>();

        return serviceCollection;
    }
}