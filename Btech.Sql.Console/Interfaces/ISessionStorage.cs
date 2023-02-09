namespace Btech.Sql.Console.Interfaces;

public interface ISessionStorage<TEntity> where TEntity : class
{
    Task<bool> SaveAsync(string email, TEntity data);

    Task<bool> UpdateAsync(string email, TEntity updatedSessionData);

    Task<bool> DeleteAsync(string email);

    Task<TEntity> GetAsync(string email);

    Task<long> CountAsync();
}