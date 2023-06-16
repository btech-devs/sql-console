using Btech.Core.Database.Interfaces;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Database;

namespace Btech.Sql.Console.DataStorages.Query;

/// <summary>
/// Represents a saved query storage implementation that uses a database.
/// </summary>
public class DatabaseSavedQueryStorage : ISavedQueryStorage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSavedQueryStorage"/> class with the specified unit of work factory.
    /// </summary>
    /// <param name="unitOfWorkFactory">The unit of work factory to be used for database operations.</param>
    public DatabaseSavedQueryStorage(IUnitOfWorkFactory unitOfWorkFactory)
    {
        this.UnitOfWorkFactory = unitOfWorkFactory;
    }

    private IUnitOfWorkFactory UnitOfWorkFactory { get; }

    // private Task<bool> SaveAsync(string email, List<QueryData> data) => throw new NotImplementedException();

    /// <inheritdoc />
    public async Task<bool> SaveAsync(string email, QueryData data)
    {
        bool result;

        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<SavedQuery> repository = unitOfWork.GetRepository<SavedQuery>();

            await repository.InsertAsync(
                new SavedQuery
                {
                    UserEmail = email,
                    QueryName = data.Name,
                    Query = data.Query
                });

            result = await unitOfWork.SaveChangesAsync() > 0;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(string email, QueryData data)
    {
        bool result;

        if (data.Id.HasValue)
        {
            await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
            {
                IRepository<SavedQuery> repository = unitOfWork.GetRepository<SavedQuery>();

                SavedQuery savedQuery = await repository
                    .SelectFirstOrDefaultAsync(query => query.Id == data.Id && query.UserEmail == email);

                if (savedQuery != null)
                {
                    savedQuery.QueryName = data.Name;
                    savedQuery.Query = data.Query;

                    await repository.UpdateAsync(savedQuery);
                }
                else
                {
                    await repository.InsertAsync(
                        new SavedQuery
                        {
                            UserEmail = email,
                            QueryName = data.Name,
                            Query = data.Query
                        });
                }

                result = await unitOfWork.SaveChangesAsync() > 0;
            }
        }
        else
        {
            result = await this.SaveAsync(email, data);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string email, long? id = null)
    {
        bool result = false;

        if (id.HasValue)
        {
            await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
            {
                IRepository<SavedQuery> repository = unitOfWork.GetRepository<SavedQuery>();

                await repository.DeleteIfExistsAsync(predicate: query => query.UserEmail == email && query.Id == id);

                result = await unitOfWork.SaveChangesAsync() > 0;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<QueryData>> GetAsync(string email)
    {
        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<SavedQuery> repository = unitOfWork.GetRepository<SavedQuery>();

            return (await repository.SelectAsync(predicate: query => query.UserEmail == email)).Select(
                    savedQuery => new QueryData
                    {
                        Id = savedQuery.Id,
                        Name = savedQuery.QueryName,
                        Query = savedQuery.Query
                    })
                .ToList();
        }
    }

    /// <inheritdoc />
    public async Task<QueryData> GetAsync(string email, long id)
    {
        QueryData queryData = null;

        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<SavedQuery> repository = unitOfWork.GetRepository<SavedQuery>();

            SavedQuery savedQuery = await repository
                .SelectFirstOrDefaultAsync(predicate: query => query.UserEmail == email && query.Id == id);

            if (savedQuery != null)
            {
                queryData = new QueryData
                {
                    Id = savedQuery.Id,
                    Name = savedQuery.QueryName,
                    Query = savedQuery.Query
                };
            }
        }

        return queryData;
    }
}