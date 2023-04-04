using Btech.Core.Database.Interfaces;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace Btech.Sql.Console.DataStorages;

public class DatabaseSessionStorage : ISessionStorage<SessionData>
{
    public DatabaseSessionStorage(ILogger<DatabaseSessionStorage> logger, IUnitOfWorkFactory unitOfWorkFactory)
    {
        this.Logger = logger;
        this.UnitOfWorkFactory = unitOfWorkFactory;
    }

    private IUnitOfWorkFactory UnitOfWorkFactory { get; }
    private ILogger Logger { get; }

    #region Public Methods

    public async Task<bool> SaveAsync(string email, SessionData data)
    {
        // TODO: shouldn’t we check if a user already has a connection to a specific instance and close an existing one?
        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<UserSession> authSessionRepository = unitOfWork.GetRepository<UserSession>();

            await authSessionRepository.InsertAsync(
                new UserSession
                {
                    Email = email,
                    AccessToken = data.AccessToken,
                    IdToken = data.IdToken,
                    RefreshToken = data.RefreshToken,
                    DbSessions = data.TransformToDatabaseSessions(email)
                });

            await unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> UpdateAsync(string email, SessionData updatedSessionData)
    {
        bool result = false;

        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<UserSession> authSessionRepository = unitOfWork.GetRepository<UserSession>();

            UserSession userSession = await authSessionRepository
                .SelectFirstOrDefaultAsync(
                    predicate: authSession => authSession.Email == email,
                    include: userSessions =>
                        userSessions.Include(userSession => userSession.DbSessions));

            if (userSession != null)
            {
                userSession.AccessToken = updatedSessionData.AccessToken;
                userSession.IdToken = updatedSessionData.IdToken;
                userSession.RefreshToken = updatedSessionData.RefreshToken;

                List<DatabaseSession> newDbSessions = updatedSessionData.TransformToDatabaseSessions(email);

                List<DatabaseSession> dbSessionsToDelete = new();

                foreach (DatabaseSession existingDbSession in userSession.DbSessions)
                {
                    if (newDbSessions.All(newSession => newSession.AccessToken != existingDbSession.AccessToken))
                        dbSessionsToDelete.Add(existingDbSession);
                }

                userSession.DbSessions = userSession.DbSessions
                    .Where(savedSession => !dbSessionsToDelete.Contains(savedSession))
                    .ToList();

                foreach (DatabaseSession newDbSession in newDbSessions)
                {
                    DatabaseSession existingDbSession = userSession.DbSessions
                        .FirstOrDefault(session => session.AccessToken == newDbSession.AccessToken);

                    if (existingDbSession != null)
                        existingDbSession.RefreshToken = newDbSession.RefreshToken;
                    else
                        userSession.DbSessions.Add(newDbSession);
                }

                if (dbSessionsToDelete.Any())
                {
                    await unitOfWork
                        .GetRepository<DatabaseSession>()
                        .DeleteAsync(
                            entities: dbSessionsToDelete);
                }

                await unitOfWork.SaveChangesAsync();

                result = true;
            }
        }

        return result;
    }

    public async Task<bool> DeleteAsync(string email)
    {
        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<UserSession> authSessionRepository = unitOfWork.GetRepository<UserSession>();

            UserSession userSession = await authSessionRepository
                .SelectFirstOrDefaultAsync(authSession => authSession.Email == email);

            if (userSession != null)
            {
                await authSessionRepository.DeleteAsync(userSession);
            }

            await unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<SessionData> GetAsync(string email)
    {
        SessionData sessionData = null;

        await using (IUnitOfWork unitOfWork = this.UnitOfWorkFactory.GetUnitOfWork())
        {
            IRepository<UserSession> authSessionRepository = unitOfWork.GetRepository<UserSession>();

            UserSession userSession = await authSessionRepository
                .SelectFirstOrDefaultAsync(
                    predicate: authSession => authSession.Email == email,
                    include: sessions =>
                        sessions.Include(authSession => authSession.DbSessions));

            if (userSession != null)
            {
                sessionData = new SessionData(userSession.AccessToken, userSession.IdToken, userSession.RefreshToken);

                if (userSession.DbSessions?.Any() is true)
                {
                    sessionData.DbSessions = new Dictionary<string, DbSession>();

                    foreach (DatabaseSession dbSession in userSession.DbSessions)
                    {
                        sessionData.DbSessions[dbSession.AccessToken] = new DbSession
                        {
                            ConnectionString = dbSession.ConnectionString,
                            RefreshToken = dbSession.RefreshToken
                        };
                    }
                }
            }
        }

        return sessionData;
    }

    #endregion Public Methods
}