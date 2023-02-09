using Newtonsoft.Json;

namespace Btech.Sql.Console.Models;

public class SessionData
{
    public string AccessToken { get; set; }

    public string IdToken { get; set; }

    public string RefreshToken { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, DbSession> DbSessions { get; set; }

    [JsonIgnore]
    public int AuthorizedDbSessionCount => this.DbSessions?.Count ?? 0;

    public SessionData(string accessToken, string idToken, string refreshToken)
    {
        this.AccessToken = accessToken;
        this.IdToken = idToken;
        this.RefreshToken = refreshToken;
    }

    /// <summary>
    /// Get DbSession.
    /// </summary>
    /// <param name="sessionKey">The key of the session.</param>
    /// <returns>Database session.</returns>
    public DbSession GetDbSession(string sessionKey)
    {
        DbSession dbSession = null;

        if (this.AuthorizedDbSessionCount > 0)
        {
            this.DbSessions?.TryGetValue(sessionKey, out dbSession);

            // if (dbSession is null)
            // {
            //     throw new ApplicationException("DbSession data is missed.");
            // }
        }

        return dbSession;
    }

    /// <summary>
    /// Finish and delete a database session.
    /// </summary>
    /// <param name="sessionKey">The key of the session to be removed.</param>
    /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
    public bool DeleteDbSession(string sessionKey)
    {
        bool result = false;

        if (this.DbSessions?.Any() is true)
        {
            result = this.DbSessions?.Remove(sessionKey, out DbSession _) ?? false;
        }

        return result;
    }

    /// <summary>
    /// Create new session for specified connection string.
    /// </summary>
    /// <param name="sessionKey">The generated jwt-token representing the session.</param>
    /// <param name="refreshToken">The generated jwt-token used for session prolongation.</param>
    /// <param name="connectionString">Built connection string.</param>
    /// <returns>True if the session is successfully saved; otherwise, false.</returns>
    public bool CreateDbSession(string sessionKey, string refreshToken, string connectionString)
    {
        this.DbSessions ??= new Dictionary<string, DbSession>();

        DbSession dbSession = new DbSession
        {
            ConnectionString = connectionString,
            RefreshToken = refreshToken
        };

        return this.DbSessions?.TryAdd(sessionKey, dbSession) ?? false;
    }
}