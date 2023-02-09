using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Btech.Core.Database.Base;
using Btech.Core.Database.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace Btech.Core.Database;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : EntityBase, new()
{
    public Repository(DbContext dbContext) =>
        this.DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), $"Could not initialize '{typeof(Repository<TEntity>).FullName}'.");

    public DbContext DbContext { get; }

    private static int CalculateSkip(int? page, int? count) => (page.Value - 1) * count.Value;

    #region Public Methods

    public async Task<TEntity> SelectFirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        bool disableTracking = false)
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        return await query.FirstOrDefaultAsync();
    }

    public async Task<TEntity> SelectFirstOrDefaultAsync<TIncluded>(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TIncluded>> include = null,
        bool disableTracking = false)
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        if (include != null)
            query = include(query);

        return await query.FirstOrDefaultAsync();
    }

    public Task<IQueryable<TEntity>> SelectAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        bool disableTracking = false,
        int? page = null,
        int? offset = null,
        int? count = null)
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        if (offset != null || page != null && count != null)
            query = query.Skip(offset ?? CalculateSkip(page, count));

        if (count != null)
            query = query.Take(count.Value);

        return Task.FromResult(query);
    }

    public Task<IQueryable<TResult>> SelectAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        bool disableTracking = false,
        int? page = null,
        int? offset = null,
        int? count = null)
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        IQueryable<TResult> selectedQuery = query.Select(selector).Distinct();

        if (offset != null || page != null && count != null)
            selectedQuery = selectedQuery.Skip(offset ?? CalculateSkip(page, count));

        if (count != null)
            selectedQuery = selectedQuery.Take(count.Value);

        return Task.FromResult(selectedQuery);
    }

    public Task<IQueryable<TEntity>> SelectAsync<TIncluded>(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, TIncluded>> include = null,
        bool disableTracking = false,
        int? page = null,
        int? count = null)
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (disableTracking)
            query = query.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        if (include != null)
            query = include(query);

        if (page != null && count != null)
            query = query.Skip(CalculateSkip(page, count));

        if (count != null)
            query = query.Take(count.Value);

        return Task.FromResult(query);
    }

    public Task<IQueryable<TResult>> SelectWithJoinAsync<TJoinedEntity, TKey, TResult>(
        Expression<Func<TEntity, TKey>> outerKeySelector,
        Expression<Func<TJoinedEntity, TKey>> innerKeySelector,
        Expression<Func<TEntity, TJoinedEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>> outerPredicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> outerOrderBy = null,
        Expression<Func<TResult, bool>> resultPredicate = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>> resultOrderBy = null,
        int? page = null,
        int? count = null)
        where TJoinedEntity : EntityBase, new()
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (outerPredicate != null)
            query = query.Where(outerPredicate);

        if (outerOrderBy != null)
            query = outerOrderBy(query);

        IQueryable<TResult> resultQuery = query
            .Join(
                inner: this.DbContext.Set<TJoinedEntity>(),
                outerKeySelector: outerKeySelector,
                innerKeySelector: innerKeySelector,
                resultSelector: resultSelector);

        if (resultPredicate != null)
            resultQuery = resultQuery.Where(resultPredicate);

        if (resultOrderBy != null)
            resultQuery = resultOrderBy(resultQuery);

        if (page != null && count != null)
            resultQuery = resultQuery.Skip(CalculateSkip(page, count));

        if (count != null)
            resultQuery = resultQuery.Take(count.Value);

        return Task.FromResult(resultQuery);
    }

    public Task<long> CountWithJoinAsync<TJoinedEntity, TKey, TResult>(
        Expression<Func<TEntity, TKey>> outerKeySelector,
        Expression<Func<TJoinedEntity, TKey>> innerKeySelector,
        Expression<Func<TEntity, TJoinedEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>> outerPredicate = null,
        Expression<Func<TResult, bool>> resultPredicate = null)
        where TJoinedEntity : EntityBase, new()
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (outerPredicate != null)
            query = query.Where(outerPredicate);

        IQueryable<TResult> resultQuery = query
            .Join(
                inner: this.DbContext.Set<TJoinedEntity>(),
                outerKeySelector: outerKeySelector,
                innerKeySelector: innerKeySelector,
                resultSelector: resultSelector);

        if (resultPredicate != null)
            resultQuery = resultQuery.Where(resultPredicate);

        return resultQuery.LongCountAsync();
    }

    public Task<TResult> SelectFirstOrDefaultWithJoinAsync<TJoinedEntity, TKey, TResult>(
        Expression<Func<TEntity, TKey>> outerKeySelector,
        Expression<Func<TJoinedEntity, TKey>> innerKeySelector,
        Expression<Func<TEntity, TJoinedEntity, TResult>> resultSelector,
        Expression<Func<TEntity, bool>> outerPredicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> outerOrderBy = null,
        Expression<Func<TResult, bool>> resultPredicate = null,
        Func<IQueryable<TResult>, IOrderedQueryable<TResult>> resultOrderBy = null)
        where TJoinedEntity : EntityBase, new()
    {
        IQueryable<TEntity> query = this.DbContext.Set<TEntity>();

        if (outerPredicate != null)
            query = query.Where(outerPredicate);

        if (outerOrderBy != null)
            query = outerOrderBy(query);

        IQueryable<TResult> resultQuery = query
            .Join(
                inner: this.DbContext.Set<TJoinedEntity>(),
                outerKeySelector: outerKeySelector,
                innerKeySelector: innerKeySelector,
                resultSelector: resultSelector);

        TResult result;

        if (resultOrderBy == null)
            result = resultPredicate != null
                ? resultQuery.FirstOrDefault(resultPredicate)
                : resultQuery.FirstOrDefault();
        else
            result = resultPredicate != null
                ? resultOrderBy(resultQuery).FirstOrDefault(resultPredicate)
                : resultOrderBy(resultQuery).FirstOrDefault();

        return Task.FromResult(result);
    }

    // public Task<IQueryable<TResult>> SelectWithJoinAsync<TJoinedEntity, TJoinedResult, TKey, TResult>(
    //     Func<DbSet<TJoinedEntity>, IEnumerable<TJoinedResult>> innerEntitiesSelector,
    //     Expression<Func<TEntity, TKey>> outerKeySelector,
    //     Expression<Func<TJoinedResult, TKey>> innerKeySelector,
    //     Expression<Func<TEntity, TJoinedResult, TResult>> resultSelector,
    //     Expression<Func<TEntity, bool>> outerPredicate = null,
    //     Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> outerOrderBy = null,
    //     Expression<Func<TResult, bool>> resultPredicate = null,
    //     int? page = null,
    //     int? count = null)
    //     where TJoinedEntity : EntityBase, new()
    // {
    //     IQueryable<TEntity> query = this._dbContext.Set<TEntity>();
    //
    //     if (outerPredicate != null)
    //         query = query.Where(outerPredicate);
    //
    //     if (outerOrderBy != null)
    //         query = outerOrderBy(query);
    //
    //     IEnumerable<TJoinedResult> innerEntities = innerEntitiesSelector(this._dbContext.Set<TJoinedEntity>());
    //
    //     IQueryable<TResult> resultQuery = query
    //         .Join(
    //             inner: innerEntities,
    //             outerKeySelector: outerKeySelector,
    //             innerKeySelector: innerKeySelector,
    //             resultSelector: resultSelector);
    //
    //     if (resultPredicate != null)
    //         resultQuery = resultQuery.Where(resultPredicate);
    //
    //     if (page != null && count != null)
    //         resultQuery = resultQuery.Skip(CalculateSkip(page, count));
    //
    //     if (count != null)
    //         resultQuery = resultQuery.Take(count.Value);
    //
    //     return Task.FromResult(resultQuery);
    // }

    public async Task<TResult> SelectMaxAsync<TResult>(Expression<Func<TEntity, TResult>> selector) =>
        await this.DbContext.Set<TEntity>().MaxAsync(selector);

    public async Task<long> CountAsync(
        Expression<Func<TEntity, bool>> predicate = null)
    {
        Task<long> query = predicate switch
        {
            null => this.DbContext.Set<TEntity>().LongCountAsync(),
            _ => this.DbContext.Set<TEntity>().LongCountAsync(predicate)
        };

        return await query;
    }

    public Task<TEntity> InsertAsync(TEntity entity) =>
        Task.FromResult(this.DbContext.Add(entity).Entity);

    public async Task InsertAsync(IEnumerable<TEntity> entities) =>
        await this.DbContext.AddRangeAsync(entities);

    public Task<TEntity> UpdateAsync(TEntity entity) =>
        Task.FromResult(this.DbContext.Update(entity).Entity);

    public Task UpdateAsync(IEnumerable<TEntity> entities)
    {
        this.DbContext.UpdateRange(entities);

        return Task.CompletedTask;
    }

    public async Task<TEntity> UpdateIfExistsAsync(Expression<Func<TEntity, bool>> predicate, Action<TEntity> action)
    {
        TEntity entity = null;
        long count = await this.CountAsync(predicate);

        if (count == 1)
        {
            entity = await this.SelectFirstOrDefaultAsync(predicate);

            if (entity != null)
            {
                action(entity);
                entity = await this.UpdateAsync(entity);
            }
        }

        return entity;
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate = null)
    {
        Task<bool> query = predicate switch
        {
            null => this.DbContext.Set<TEntity>().AnyAsync(),
            _ => this.DbContext.Set<TEntity>().AnyAsync(predicate)
        };

        return await query;
    }

    public async Task DeleteAsync<TKey>(TKey key)
    {
        TypeInfo type = typeof(TEntity).GetTypeInfo();

        IProperty keyProperty = this.DbContext
            .Model
            .FindEntityType(type)
            ?.FindPrimaryKey()
            ?.Properties
            .FirstOrDefault();

        if (keyProperty != null)
        {
            PropertyInfo property = type.GetProperty(keyProperty.Name);

            if (property != null)
            {
                TEntity entity = Activator.CreateInstance<TEntity>();
                property.SetValue(entity, key);
                this.DbContext.Entry(entity).State = EntityState.Deleted;
            }
            else
            {
                TEntity entity = await this.DbContext.Set<TEntity>().FindAsync(key);

                if (entity != null)
                    await this.DeleteAsync(entity);
            }
        }
    }

    public Task DeleteAsync(TEntity entity)
    {
        this.DbContext.Remove(entity);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(IEnumerable<TEntity> entities)
    {
        this.DbContext.RemoveRange(entities);

        return Task.CompletedTask;
    }

    public async Task<bool> DeleteIfExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        bool deleted = false;
        long count = await this.CountAsync(predicate);

        if (count == 1)
        {
            TEntity entity = await this.SelectFirstOrDefaultAsync(predicate);

            if (entity != null)
            {
                await this.DeleteAsync(entity);
                deleted = true;
            }
        }

        return deleted;
    }

    #endregion Public Methods
}