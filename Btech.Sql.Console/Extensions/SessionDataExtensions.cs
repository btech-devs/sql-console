using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Database;

namespace Btech.Sql.Console.Extensions;

public static class SessionDataExtensions
{
    public static List<DatabaseSession> TransformToDatabaseSessions(this SessionData sessionData, string email)
    {
        List<DatabaseSession> dbSessions = new List<DatabaseSession>();

        if (sessionData.DbSessions?.Any() is true)
        {
            foreach (KeyValuePair<string, DbSession> dataDbSession in sessionData.DbSessions)
            {
                dbSessions.Add(
                    new DatabaseSession
                    {
                        UserEmail = email,
                        ConnectionString = dataDbSession.Value.ConnectionString,
                        AccessToken = dataDbSession.Key,
                        RefreshToken = dataDbSession.Value.RefreshToken
                    });
            }
        }

        return dbSessions;
    }
}