using System.Security.Claims;
using System.Text.Encodings.Web;
using Btech.Core.Database.Utils;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authentication.Base;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Requests.GoogleServices;
using Btech.Sql.Console.Models.Responses.GoogleServices;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;

namespace Btech.Sql.Console.Identity.Authentication;

/// <summary>
/// Authentication handler for Google identity authentication.
/// </summary>
public class GoogleIdentityAuthenticationHandler : AuthenticationHandlerBase<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleIdentityAuthenticationHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="clock">The system clock.</param>
    /// <param name="sessionStorage">The session storage.</param>
    public GoogleIdentityAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock, ISessionStorage<SessionData> sessionStorage)
        : base(options, logger, encoder, clock, sessionStorage)
    {
    }

    /// <summary>
    /// The URI for Google token refresh requests.
    /// </summary>
    private const string GoogleRefreshTokenUri = "https://oauth2.googleapis.com/token";

    /// <summary>
    /// Refreshes the access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the token response.</returns>
    private async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        RestClient client = new RestClient();

        RestRequest request = new RestRequest(GoogleRefreshTokenUri, Method.Post);

        TokenResponse tokenResponse = null;

        request = request.AddJsonBody(
            new RefreshTokenRequest
            {
                ClientId = EnvironmentUtils.GetRequiredVariable(Constants.ClientIdEnvironmentVariableName),
                ClientSecret = EnvironmentUtils.GetRequiredVariable(Constants.ClientSecretEnvironmentVariableName),
                RefreshToken = refreshToken,
                GrantType = "refresh_token"
            });

        try
        {
            RestResponse<TokenResponse> restResponse = await client.ExecuteAsync<TokenResponse>(request);
            tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(restResponse.Content!);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, $"Token refreshing failed. Exception: '{exception.Message}'.");

            await AuditNotifier.ReportExceptionAsync(exception, $"{nameof(GoogleIdentityAuthenticationHandler)}.{nameof(this.RefreshTokenAsync)}");
        }
        finally
        {
            client.Dispose();
        }

        return tokenResponse ?? new TokenResponse
        {
            Error = "Refresh token fails.",
            ErrorDescription = "UnexpectedResponse: 'restResponse.Data' is null.'."
        };
    }

    /// <summary>
    /// Creates a new authentication ticket for the specified email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The authentication ticket.</returns>
    private AuthenticationTicket CreateAuthenticationTicket(string email)
    {
        Claim[] claims =
        {
            new(Constants.Identity.ClaimTypes.Email, email)
        };

        ClaimsIdentity identity = new(claims, this.Scheme.Name);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, this.Scheme.Name);

        return ticket;
    }

    /// <summary>
    /// The <see cref="IdentityError"/> value to use when authentication fails.
    /// </summary>
    protected override IdentityError IdentityErrorValue => IdentityError.IdAuthenticationFailed;

    /// <summary>
    /// Attempts to authenticate the user.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation. The result is the authentication result.</returns>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateResult authenticateResult = AuthenticateResult.Fail("Google authentication failed.");

        string idToken = this.GetRequestHeader(Constants.Identity.HeaderNames.Request.IdTokenHeaderName);

        if (!idToken.IsNullOrEmpty())
        {
            if (this.TryGetTokenClaim(idToken, Constants.Identity.ClaimTypes.Email, out string email))
            {
                if (!email.IsNullOrEmpty())
                {
                    SessionData sessionDataRecord;

                    if ((sessionDataRecord = await this.SessionStorage.GetAsync(email)) is not null)
                    {
                        if (sessionDataRecord.IdToken == idToken)
                        {
                            if (this.TryGetTokenClaim(
                                    idToken, Constants.Identity.ClaimTypes.Expiration, out string expirationString))
                            {
                                if (!expirationString.IsNullOrEmpty())
                                {
                                    long expirationTimestamp = long.Parse(expirationString);

                                    DateTime expirationDate = DateTimeOffset.FromUnixTimeSeconds(expirationTimestamp).UtcDateTime;

                                    if (DateTime.UtcNow < expirationDate)
                                    {
                                        authenticateResult = AuthenticateResult.Success(this.CreateAuthenticationTicket(email));
                                        this.Logger.LogDebug($"Google authentication succeeded for user '{email}'.");

                                        this.Context.Items
                                            .Add(Constants.Identity.SessionDataItemName, sessionDataRecord);
                                    }
                                    else
                                    {
                                        string refreshToken = sessionDataRecord.RefreshToken;

                                        if (!refreshToken.IsNullOrEmpty())
                                        {
                                            TokenResponse tokenResponse = await this.RefreshTokenAsync(refreshToken);

                                            if (tokenResponse.IsSucceeded)
                                            {
                                                SessionData newSessionData = sessionDataRecord;
                                                newSessionData.AccessToken = tokenResponse.AccessToken;
                                                newSessionData.IdToken = tokenResponse.IdToken;

                                                if (await this.SessionStorage.UpdateAsync(email, newSessionData))
                                                {
                                                    authenticateResult = AuthenticateResult.Success(this.CreateAuthenticationTicket(email));
                                                    this.Logger.LogDebug($"Google refreshing succeeded for user '{email}'. Session has been updated.");

                                                    this.Context.Items
                                                        .Add(Constants.Identity.SessionDataItemName, newSessionData);

                                                    this.AddResponseHeader(
                                                        Constants.Identity.HeaderNames.Response.RefreshedIdTokenHeaderName,
                                                        newSessionData.IdToken);
                                                }
                                            }
                                            else
                                            {
                                                this.AddErrorHeader();
                                                this.Logger.LogError($"Google refreshing failed for user '{email}'. Error: '{tokenResponse.Error}'. ErrorDescription: '{tokenResponse.ErrorDescription}'.");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    this.AddErrorHeader();
                                    this.Logger.LogError($"'{Constants.Identity.HeaderNames.Request.IdTokenHeaderName}' was found, but '{Constants.Identity.ClaimTypes.Expiration}' claim was not provided.");
                                }
                            }
                            else
                            {
                                this.AddErrorHeader();
                                // logged earlier in TryGetTokenClaim(...)
                            }
                        }
                        else
                        {
                            this.AddErrorHeader();
                            this.Logger.LogWarning($"'{email}' session may be compromised - invalid '{Constants.Identity.HeaderNames.Request.IdTokenHeaderName}'.");

                            await this.SessionStorage.DeleteAsync(email);
                            // TODO: send email notification to user or smth else
                        }
                    }
                    else
                    {
                        this.AddErrorHeader();
                        this.Logger.LogWarning($"Session data was not found for user '{email}'.");
                    }
                }
                else
                {
                    this.AddErrorHeader();
                    this.Logger.LogError($"'{Constants.Identity.HeaderNames.Request.IdTokenHeaderName}' was found, but '{Constants.Identity.ClaimTypes.Email}' claim was not provided.");
                }
            }
            else
            {
                this.AddErrorHeader();
                // logged earlier in TryGetTokenClaim(...)
            }
        }
        else
        {
            this.AddErrorHeader();
            this.Logger.LogInformation($"'{Constants.Identity.HeaderNames.Request.IdTokenHeaderName}' was not found. Access denied.");
        }

        return authenticateResult;
    }
}