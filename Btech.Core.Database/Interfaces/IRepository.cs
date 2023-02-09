using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Btech.Core.Database.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Btech.Core.Database.Interfaces;

/// <summary>
/// Contains base common methods to work with a database.
/// </summary>
/// <typeparam name="TEntity">A database entity type.</typeparam>
public interface IRepository<TEntity> where TEntity : EntityBase, new()
{
    /// <summary>
    /// A database context object.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    DbContext DbContext { get; }

    #region Methods

    /// <summary>
    /// Selects first or default entity.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements by.</param>
    /// <param name="disableTracking">Indicates that the change tracker will not track any of the entities that are returned from a LINQ query (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>).</param>
    // ReSharper disable once UnusedMember.Global
    Task<TEntity> SelectFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        bool disableTracking = false);

    /// <summary>
    /// Selects first or default entity.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements by.</param>
    /// <param name="include">A function to include an additional entity.</param>
    /// <param name="disableTracking">Indicates that the change tracker will not track any of the entities that are returned from a LINQ query (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>).</param>
    // ReSharper disable once UnusedMember.Global
    Task<TEntity> SelectFirstOrDefaultAsync<TIncluded>(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TIncluded>> include = null,
        bool disableTracking = false);

    /// <summary>
    /// Selects entities.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements by.</param>
    /// <param name="disableTracking">Indicates that the change tracker will not track any of the entities that are returned from a LINQ query (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>).</param>
    /// <param name="page">A page number.</param>
    /// <param name="offset">An entity count that should be skipped.</param>
    /// <param name="count">An element count per page.</param>
    // ReSharper disable once UnusedMember.Global
    Task<IQueryable<TEntity>> SelectAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        bool disableTracking = false,
        int? page = null,
        int? offset = null,
        int? count = null);

    /// <summary>
    /// Selects entities.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements by.</param>
    /// <param name="include">A function to include an additional entity.</param>
    /// <param name="disableTracking">Indicates that the change tracker will not track any of the entities that are returned from a LINQ query (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>).</param>
    /// <param name="page">A page number.</param>
    /// <param name="count">An element count per page.</param>
    // ReSharper disable once UnusedMember.Global
    Task<IQueryable<TEntity>> SelectAsync<TIncluded>(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TIncluded>> include = null,
        bool disableTracking = false,
        int? page = null,
        int? count = null);

    /// <summary>
    /// Selects entities.
    /// </summary>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="orderBy">A function to order elements by.</param>
    /// <param name="disableTracking">Indicates that the change tracker will not track any of the entities that are returned from a LINQ query (<see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>).</param>
    /// <param name="page">A page number.</param>
    /// <param name="offset">An entity count that should be skipped.</param>
    /// <param name="count">An element count per page.</param>
    Task<IQueryable<TResult>> SelectAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        bool disableTracking = false,
        int? page = null,
        int? offset = null,
        int? count = null);

    /// <summary>
    /// Selects entities joining additional entities.
    /// </summary>
    /// <param name="outerKeySelector">A function to extract a join key from each element of a first sequence.</param>
    /// <param name="innerKeySelector">A function to extract a join key from each element of a second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <param name="outerPredicate">A function to test each element of a first sequence for a condition.</param>
    /// <param name="outerOrderBy">A function to order elements of a first sequence by.</param>
    /// <param name="resultPredicate">A function to test each element of a result sequence for a condition.</param>
    /// <param name="resultOrderBy">A function to order result elements by.</param>
    /// <param name="page">A page number.</param>
    /// <param name="count">An element count per page.</param>
    /// <typeparam name="TJoinedEntity">A joined entity type.</typeparam>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TResult">A result type.</typeparam>
    // ReSharper disable once UnusedMember.Global
    Task<IQueryable<TResult>> SelectWithJoinAsync<TJoinedEntity, TKey, TResult>(
        Expression<Func<TEntity, TKey>> outerKeySelector,
        Expression<Func<TJoinedEntity, TKey>> innerKeySelector,
        Expression<Func<TEntity, TJoinedEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>> outerPredicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> outerOrderBy = null,
        Expression<Func<TResult, bool>> resultPredicate = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>> resultOrderBy = null,
        int? page = null,
        int? count = null)
        where TJoinedEntity : EntityBase, new();

    /// <summary>
    /// Counts entities joining additional entities.
    /// </summary>
    /// <param name="outerKeySelector">A function to extract a join key from each element of a first sequence.</param>
    /// <param name="innerKeySelector">A function to extract a join key from each element of a second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <param name="outerPredicate">A function to test each element of a first sequence for a condition.</param>
    /// <param name="resultPredicate">A function to test each element of a result sequence for a condition.</param>
    /// <typeparam name="TJoinedEntity">A joined entity type.</typeparam>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TResult">A result type.</typeparam>
    Task<long> CountWithJoinAsync<TJoinedEntity, TKey, TResult>(
        Expression<Func<TEntity, TKey>> outerKeySelector,
        Expression<Func<TJoinedEntity, TKey>> innerKeySelector,
        Expression<Func<TEntity, TJoinedEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>> outerPredicate = null,
        Expression<Func<TResult, bool>> resultPredicate = null)
        where TJoinedEntity : EntityBase, new();

    /// <summary>
    /// Selects first or default entity joining additional entity.
    /// </summary>
    /// <param name="outerKeySelector">A function to extract a join key from each element of a first sequence.</param>
    /// <param name="innerKeySelector">A function to extract a join key from each element of a second sequence.</param>
    /// <param name="resultSelector">A function to create a result element from two matching elements.</param>
    /// <param name="outerPredicate">A function to test each element of a first sequence for a condition.</param>
    /// <param name="outerOrderBy">A function to order elements of a first sequence by.</param>
    /// <param name="resultPredicate">A function to test each element of a result sequence for a condition.</param>
    /// <param name="resultOrderBy">A function to order result elements by.</param>
    /// <typeparam name="TJoinedEntity">A joined entity type.</typeparam>
    /// <typeparam name="TKey">A key type.</typeparam>
    /// <typeparam name="TResult">A result type.</typeparam>
    /// <returns></returns>
    Task<TResult> SelectFirstOrDefaultWithJoinAsync<TJoinedEntity, TKey, TResult>(
        Expression<Func<TEntity, TKey>> outerKeySelector,
        Expression<Func<TJoinedEntity, TKey>> innerKeySelector,
        Expression<Func<TEntity, TJoinedEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>> outerPredicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> outerOrderBy = null,
        Expression<Func<TResult, bool>> resultPredicate = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>> resultOrderBy = null)
        where TJoinedEntity : EntityBase, new();

    // Task<IQueryable<TResult>> SelectWithJoinAsync<TJoinedEntity, TJoinedResult, TKey, TResult>(
    //     Func<DbSet<TJoinedEntity>, IEnumerable<TJoinedResult>> innerEntitiesSelector,
    //     Expression<Func<TEntity, TKey>> outerKeySelector,
    //     Expression<Func<TJoinedResult, TKey>> innerKeySelector,
    //     Expression<Func<TEntity, TJoinedResult, TResult>> resultSelector,
    //     Expression<Func<TEntity, bool>> outerPredicate = null,
    //     Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> outerOrderBy = null,
    //     Expression<Func<TResult, bool>> resultPredicate = null,
    //     int? page = null,
    //     int? count = null)
    //     where TJoinedEntity : EntityBase, new();

    /// <summary>
    /// Selects a max value.
    /// </summary>
    /// <param name="selector">A projection function to apply to each element.</param>
    /// <typeparam name="TResult">A result type.</typeparam>
    // ReSharper disable once UnusedMember.Global
    Task<TResult> SelectMaxAsync<TResult>([NotNull] Expression<Func<TEntity, TResult>> selector);

    /// <summary>
    /// Gets count of a sequence.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    // ReSharper disable once UnusedMember.Global
    Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate = null);

    /// <summary>
    /// Inserts an new <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">A new entity.</param>
    // ReSharper disable once UnusedMember.Global
    Task<TEntity> InsertAsync([NotNull] TEntity entity);

    /// <summary>
    /// Inserts <paramref name="entities"/>.
    /// </summary>
    /// <param name="entities">An new entity list.</param>
    // ReSharper disable once UnusedMember.Global
    Task InsertAsync([NotNull] IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates an existing <paramref name="entity"/>.
    /// </summary>
    /// <param name="entity">An existing entity.</param>
    // ReSharper disable once UnusedMember.Global
    Task<TEntity> UpdateAsync([NotNull] TEntity entity);

    /// <summary>
    /// Updates existing <paramref name="entities"/>.
    /// </summary>
    /// <param name="entities">An existing entity list.</param>
    // ReSharper disable once UnusedMember.Global
    Task UpdateAsync([NotNull] IEnumerable<TEntity> entities);

    /// <summary>
    /// Updates an only entity that matches <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <param name="action">An action to be applied to an entity.</param>
    /// <returns></returns>
    Task<TEntity> UpdateIfExistsAsync(
        [NotNull] Expression<Func<TEntity, bool>> predicate, [NotNull] Action<TEntity> action); // TODO: int chunkSize = 1000

    /// <summary>
    /// Checks if there is an entity that matches a <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    // ReSharper disable once UnusedMember.Global
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate = null);

    /// <summary>
    /// Deletes an entity by its key.
    /// </summary>
    /// <param name="key">An entity key.</param>
    /// <typeparam name="TKey">A key type.</typeparam>
    // ReSharper disable once UnusedMember.Global
    Task DeleteAsync<TKey>([NotNull] TKey key);

    /// <summary>
    /// Deletes an existing entity.
    /// </summary>
    /// <param name="entity">An existing entity.</param>
    Task DeleteAsync([NotNull] TEntity entity);

    /// <summary>
    /// Deletes an existing entity list.
    /// </summary>
    /// <param name="entities">An existing entity list.</param>
    Task DeleteAsync([NotNull] IEnumerable<TEntity> entities);

    /// <summary>
    /// Deletes an only entity that matches <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns><tt>true</tt> if an entity is deleted; otherwise, <tt>false</tt>.</returns>
    Task<bool> DeleteIfExistsAsync([NotNull] Expression<Func<TEntity, bool>> predicate); // TODO: int chunkSize = 1000

    #endregion Methods
}