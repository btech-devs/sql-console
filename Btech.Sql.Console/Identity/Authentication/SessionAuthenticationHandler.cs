using System.Security.Claims;
using System.Text.Encodings.Web;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authentication.Base;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Btech.Sql.Console.Identity.Authentication;

public class SessionAuthenticationHandler : AuthenticationHandlerBase<AuthenticationSchemeOptions>
{
    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock, JwtProvider jwtProvider, ISessionStorage<SessionData> sessionStorage)
        : base(options, logger, encoder, clock, sessionStorage)
    {
        this.JwtProvider = jwtProvider;
    }

    private JwtProvider JwtProvider { get; }

    private AuthenticationTicket CreateAuthenticationTicket(string instanceType, string connectionString, string host)
    {
        Claim[] claims =
        {
            new(Constants.Identity.ClaimTypes.ConnectionString, connectionString),
            new(Constants.Identity.ClaimTypes.InstanceType, instanceType),
            new(Constants.Identity.ClaimTypes.Host, host)
        };

        ClaimsIdentity identity = new(claims, this.Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, this.Scheme.Name);

        return ticket;
    }

    protected override IdentityError IdentityErrorValue => IdentityError.SessionAuthenticationFailed;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateResult authenticateResult = AuthenticateResult.Fail("Session authentication failed.");

        string sessionToken = this.GetRequestHeader(Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName);

        if (!sessionToken.IsNullOrEmpty())
        {
            this.TryGetTokenClaim(
                this.GetRequestHeader(Constants.Identity.HeaderNames.Request.IdTokenHeaderName),
                Constants.Identity.ClaimTypes.Email, out string email);

            if (this.Context.Items[Constants.Identity.SessionDataItemName] is SessionData sessionData)
            {
                DbSession dbSession = sessionData.GetDbSession(sessionToken);

                if (dbSession is null)
                {
                    this.AddErrorHeader();
                    this.Logger.LogError("Provided session key does not exist.");
                }
                else
                {
                    string connectionString = dbSession.ConnectionString;

                    if (!connectionString.IsNullOrEmpty())
                    {
                        if (this.JwtProvider.IsValidToken(sessionToken, out bool isExpiredToken))
                        {
                            this.TryGetTokenClaim(
                                sessionToken,
                                Constants.Identity.ClaimTypes.InstanceType,
                                out string requestTokenInstanceType);

                            if (!isExpiredToken)
                            {
                                this.TryGetTokenClaim(
                                    sessionToken, Constants.Identity.ClaimTypes.Host,
                                    out string requestTokenHost);

                                authenticateResult = AuthenticateResult
                                    .Success(this.CreateAuthenticationTicket(
                                        requestTokenInstanceType, connectionString, requestTokenHost));

                                this.Logger.LogDebug("Session authentication succeeded.");
                            }
                            else
                            {
                                this.Logger.LogInformation($"'{Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName}' is expired. Refresh initiated.");

                                string refreshTokenHeader = this.GetRequestHeader(Constants.Identity.HeaderNames.Request.DbRefreshTokenHeaderName);

                                if (!refreshTokenHeader.IsNullOrEmpty())
                                {
                                    if (this.JwtProvider.IsValidToken(refreshTokenHeader, out bool isRefreshExpired))
                                    {
                                        if (!isRefreshExpired)
                                        {
                                            string storedToken = dbSession.RefreshToken;

                                            if (storedToken == refreshTokenHeader)
                                            {
                                                this.TryGetTokenClaim(
                                                    sessionToken, Constants.Identity.ClaimTypes.Host,
                                                    out string requestTokenHost);

                                                string newSessionToken = this.JwtProvider
                                                    .CreateSessionToken(
                                                        instanceType: requestTokenInstanceType,
                                                        host: requestTokenHost);

                                                string newRefreshToken = this.JwtProvider.CreateRefreshToken();

                                                if (sessionData.CreateDbSession(
                                                        newSessionToken, newRefreshToken, connectionString))
                                                {
                                                    sessionData.DeleteDbSession(sessionToken);
                                                    await this.SessionStorage.UpdateAsync(email, sessionData);

                                                    this.TryGetTokenClaim(sessionToken, Constants.Identity.ClaimTypes.InstanceType, out string instanceType);

                                                    authenticateResult = AuthenticateResult
                                                        .Success(this.CreateAuthenticationTicket(instanceType, connectionString, requestTokenHost));

                                                    this.Context.Items[Constants.Identity.SessionDataItemName] = sessionData;

                                                    this.AddResponseHeader(Constants.Identity.HeaderNames.Response.RefreshedSessionTokenHeaderName, newSessionToken);
                                                    this.AddResponseHeader(Constants.Identity.HeaderNames.Response.RefreshedRefreshTokenHeaderName, newRefreshToken);

                                                    this.Logger.LogInformation("Session refreshing succeeded, authentication succeeded.");
                                                }
                                                else
                                                {
                                                    this.AddErrorHeader();
                                                    this.Logger.LogError("Refreshing failed. Authentication needed.");
                                                }
                                            }
                                            else
                                            {
                                                this.AddErrorHeader();

                                                this.Logger.LogWarning($"'{email}' session '{sessionToken}' may be compromised - not expected refresh token.");

                                                // sessionData.DeleteDbSession(sessionToken);
                                                // await this.SessionStorage.UpdateAsync(email, sessionData);
                                                // TODO session compromised, interrupt user session and send notification ???
                                            }
                                        }
                                        else
                                        {
                                            sessionData.DeleteDbSession(sessionToken);
                                            await this.SessionStorage.UpdateAsync(email, sessionData);
                                            this.AddErrorHeader();

                                            this.Logger.LogInformation($"'{Constants.Identity.HeaderNames.Request.DbRefreshTokenHeaderName}' is expired, authentication needed.");
                                        }
                                    }
                                    else
                                    {
                                        this.AddErrorHeader();

                                        this.Logger.LogWarning($"'{email}' session '{sessionToken}' may be compromised - invalid refresh token.");

                                        // sessionData.DeleteDbSession(sessionToken);
                                        // await this.SessionStorage.UpdateAsync(email, sessionData);
                                        // TODO session compromised, ??? interrupt user session ??? and send notification
                                    }
                                }
                                else
                                {
                                    this.AddErrorHeader();
                                    this.Logger.LogInformation($"'{Constants.Identity.HeaderNames.Request.DbRefreshTokenHeaderName}' is not provided, refresh aborted.");
                                }
                            }
                        }
                        else
                        {
                            this.AddErrorHeader();
                            this.Logger.LogInformation($"'{Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName}' is invalid.");
                        }
                    }
                    else
                    {
                        this.AddErrorHeader();
                        this.Logger.LogError("Session does not exist.");
                    }
                }
            }
            else
            {
                this.AddErrorHeader();
                this.Logger.LogError("Session data was not found.");
            }
        }
        else
        {
            this.AddErrorHeader();
            this.Logger.LogInformation($"'{Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName}' was not found. Access denied.");
        }

        return authenticateResult;
    }
}