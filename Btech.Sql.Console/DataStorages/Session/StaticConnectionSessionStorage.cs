using Btech.Core.Database.Utils;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Utils;

namespace Btech.Sql.Console.DataStorages.Session;

/// <summary>
/// Implementation of ISessionStorage interface that uses environment and cookies (HttpOnly) to store session data.
/// </summary>
public class StaticConnectionSessionStorage : ISessionStorage<SessionData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StaticConnectionSessionStorage"/> class with the specified <see cref="IHttpContextAccessor"/>.
    /// </summary>
    /// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/> used to access the current HttpContext.</param>
    public StaticConnectionSessionStorage(IHttpContextAccessor httpContextAccessor)
    {
        this.HttpContextAccessor = httpContextAccessor;
    }

    private IHttpContextAccessor HttpContextAccessor { get; }

    /// <inheritdoc />
    public Task<bool> SaveAsync(string email, SessionData data)
    {
        this.HttpContextAccessor
            .HttpContext
            ?.Response
            .Cookies
            .Append(
                key: Constants.Identity.CookieNames.RefreshTokenCookieName,
                value: data.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true
                });

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> UpdateAsync(string email, SessionData updatedSessionData) => Task.FromResult(true);

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string email)
    {
        this.HttpContextAccessor
            .HttpContext
            ?.Response
            .Cookies
            .Delete(Constants.Identity.CookieNames.RefreshTokenCookieName);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<SessionData> GetAsync(string email)
    {
        string idToken = this.HttpContextAccessor
            .HttpContext
            ?.Request
            .Headers[Constants.Identity.HeaderNames.Request.IdTokenHeaderName]
            .ToString();

        string host = EnvironmentUtils.GetRequiredVariable(Constants.Identity.StaticConnectionEnvironmentVariables.Host);

        int? port = Environment
            .GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.Port)
            ?.ToNullableInt();

        string userName = EnvironmentUtils.GetRequiredVariable(Constants.Identity.StaticConnectionEnvironmentVariables.User);
        string password = EnvironmentUtils.GetRequiredVariable(Constants.Identity.StaticConnectionEnvironmentVariables.Password);

        InstanceType instanceType = Enum
            .Parse<InstanceType>(
                value: EnvironmentUtils.GetRequiredVariable(Constants.Identity.StaticConnectionEnvironmentVariables.InstanceType)!,
                ignoreCase: true);

        string connectionString = ConnectionStringBuilder
            .CreateConnectionString(
                instanceType: instanceType,
                host: host,
                port: port ?? (instanceType == InstanceType.PgSql ? 5432 : 1433),
                username: userName,
                password: password);

        SessionData sessionData = null;

        if (!idToken.IsNullOrEmpty())
        {
            sessionData = new SessionData
            {
                IdToken = idToken,
                RefreshToken = this.HttpContextAccessor
                    .HttpContext
                    ?.Request
                    .Cookies[Constants.Identity.CookieNames.RefreshTokenCookieName],
                DbSessions = new Dictionary<string, DbSession>()
            };

            string sessionToken = this.HttpContextAccessor
                .HttpContext
                ?.Request
                .Headers[Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName]
                .ToString();

            string refreshToken = this.HttpContextAccessor
                .HttpContext
                ?.Request
                .Headers[Constants.Identity.HeaderNames.Request.DbRefreshTokenHeaderName]
                .ToString();

            if (!sessionToken.IsNullOrEmpty())
                sessionData.DbSessions = new Dictionary<string, DbSession>
                {
                    {
                        sessionToken!,
                        new DbSession
                        {
                            RefreshToken = refreshToken,
                            ConnectionString = connectionString
                        }
                    }
                };
        }

        return Task.FromResult(sessionData);
    }
}