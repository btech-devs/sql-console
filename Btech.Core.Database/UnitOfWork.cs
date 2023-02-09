using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Btech.Core.Database.Base;
using Btech.Core.Database.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Btech.Core.Database;

public class UnitOfWork<TDbContext> : IUnitOfWork where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly IDictionary<string, dynamic> _repositories;
    private bool _disposed;

    public UnitOfWork(TDbContext dbContext)
    {
        this._dbContext = dbContext ??
                          throw new ArgumentNullException(
                              paramName: nameof(dbContext),
                              message: $"Could not initialize '{nameof(UnitOfWork<TDbContext>)}'.");

        this._repositories = new ConcurrentDictionary<string, object>();
    }

    #region Public Override Methods

    public IRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase, new()
    {
        IRepository<TEntity> repository;
        string repositoryKey = typeof(TEntity).FullName ?? typeof(TEntity).Name;

        if (this._repositories.ContainsKey(repositoryKey))
            repository = (IRepository<TEntity>) this._repositories[repositoryKey];
        else
        {
            repository = new Repository<TEntity>(this._dbContext);
            this._repositories.TryAdd(repositoryKey, repository);
        }

        return repository;
    }

    public async Task<int> SaveChangesAsync() => await this._dbContext.SaveChangesAsync();

    public void Dispose()
    {
        if (!this._disposed)
        {
            this._disposed = true;
            this._repositories.Clear();
            this._dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            this._disposed = true;
            this._repositories.Clear();
            await this._dbContext.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    #endregion Public Override Methods
}