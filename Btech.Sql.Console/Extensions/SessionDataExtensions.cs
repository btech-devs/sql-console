using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Database;

namespace Btech.Sql.Console.Extensions;

/// <summary>
/// Contains extension methods for <see cref="SessionData"/> class.
/// </summary>
public static class SessionDataExtensions
{
    /// <summary>
    /// Transforms the <see cref="SessionData"/> to a list of <see cref="DatabaseSession"/> instances.
    /// </summary>
    /// <param name="sessionData">The <see cref="SessionData"/> instance to transform.</param>
    /// <param name="email">The email of the user.</param>
    /// <returns>A list of <see cref="DatabaseSession"/> instances.</returns>
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