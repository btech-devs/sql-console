using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Btech.Core.Database.Base;
using Btech.Core.Database.Configurations;
using DbUp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Btech.Core.Database;

public class PgSqlDbContext : DbContext
{
    private readonly DatabaseConfiguration _databaseConfiguration;
    private readonly IList<Assembly> _migrationAssemblies;
    private readonly IList<EntityBase> _modelEntities;

    public PgSqlDbContext(
        DatabaseConfiguration databaseConfiguration, IList<Assembly> migrationAssemblies, IList<EntityBase> modelEntities)
    {
        this._databaseConfiguration = databaseConfiguration;
        this._migrationAssemblies = migrationAssemblies;
        this._modelEntities = modelEntities;

        this.UpdateDatabase();
    }

    private void UpdateDatabase()
    {
        EnsureDatabase.For.PostgresqlDatabase(this._databaseConfiguration.ConnectionString);

        DeployChanges.To.PostgresqlDatabase(this._databaseConfiguration.ConnectionString)
            .WithScriptsEmbeddedInAssemblies(
                assemblies: this._migrationAssemblies.ToArray(),
                filter: filename =>
                {
                    bool isMigration = filename.Contains("Migrations", StringComparison.OrdinalIgnoreCase) &&
                                       filename.Contains("Scripts", StringComparison.OrdinalIgnoreCase) &&
                                       filename.EndsWith(".pgsql");

                    return isMigration;
                })
            .LogToAutodetectedLog()
            .WithExecutionTimeout(new TimeSpan(0, 15, 0))
            .WithTransactionPerScript()
            .Build()
            .PerformUpgrade();
    }

    #region Override Methods

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder
            .UseNpgsql(
                connectionString: this._databaseConfiguration.ConnectionString,
                npgsqlOptionsAction: options =>
                {
                    options.CommandTimeout(this._databaseConfiguration.CommandTimeout);
                    options.MaxBatchSize(this._databaseConfiguration.MaxBatchSize);
                    options.EnableRetryOnFailure();
                });
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (EntityBase entity in this._modelEntities)
            entity.Setup(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (EntityEntry<EntityBase> entity in this.ChangeTracker.Entries<EntityBase>())
            entity.Entity.BeforeSaveChanges(entity.State);

        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (EntityEntry<EntityBase> entity in this.ChangeTracker.Entries<EntityBase>())
            entity.Entity.BeforeSaveChanges(entity.State);

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    #endregion Override Methods
}