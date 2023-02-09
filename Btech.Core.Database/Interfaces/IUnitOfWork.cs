using System;
using System.Threading.Tasks;
using Btech.Core.Database.Base;

namespace Btech.Core.Database.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets a repository for a specific entity.
    /// </summary>
    /// <typeparam name="TEntity">A database entity type.</typeparam>
    // ReSharper disable once UnusedMember.Global
    IRepository<TEntity> GetRepository<TEntity>() where TEntity : EntityBase, new();

    /// <summary>
    /// Saves all changes made in this context to a database.
    /// </summary>
    /// <returns>A number of state entries written to a database.</returns>
    // ReSharper disable once UnusedMember.Global
    Task<int> SaveChangesAsync();
}