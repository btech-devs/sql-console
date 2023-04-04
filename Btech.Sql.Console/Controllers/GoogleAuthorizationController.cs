using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Web;
using Btech.Sql.Console.Attributes;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Requests.GoogleAuthorization;
using Btech.Sql.Console.Models.Requests.GoogleServices;
using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Models.Responses.GoogleAuthorization;
using Btech.Sql.Console.Models.Responses.GoogleServices;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using RestSharp;
using RestSharp.Serializers.NewtonsoftJson;

namespace Btech.Sql.Console.Controllers;

[Controller]
[Route("api/google-auth")]
public class GoogleAuthorizationController : UserAuthorizedControllerBase
{
    public GoogleAuthorizationController(
        ILogger<GoogleAuthorizationController> logger, ISessionStorage<SessionData> sessionStorage,
        IamAuthorizationService iamAuthorizationService, GoogleProjectConfiguration googleProjectConfiguration)
        : base(logger)
    {
        this.IamAuthorizationService = iamAuthorizationService;
        this.GoogleProjectConfiguration = googleProjectConfiguration;
        this.SessionStorage = sessionStorage;
    }

    private ISessionStorage<SessionData> SessionStorage { get; }

    private IamAuthorizationService IamAuthorizationService { get; }

    private GoogleProjectConfiguration GoogleProjectConfiguration { get; }

    private async Task RevokeTokenAsync(string token)
    {
        if (!token.IsNullOrEmpty())
        {
            RestClient client = new RestClient();
            client.UseNewtonsoftJson();

            RestRequest request = new RestRequest("https://oauth2.googleapis.com/revoke", Method.Post);

            request = request.AddParameter("token", token);
            request = request.AddOrUpdateHeader("Content-type", "application/x-www-form-urlencoded");

            try
            {
                await client.ExecuteAsync<ResponseBase>(request);
            }
            catch (Exception exception)
            {
                this.LogError(exception, "Error on token revoking.");

                await AuditNotifier.ReportExceptionAsync(exception, $"{nameof(GoogleAuthorizationController)}.{nameof(this.RevokeTokenAsync)}");
            }
            finally
            {
                client.Dispose();
            }
        }
    }

    [HttpPost("exchange-code")]
    [AllowAnonymous]
    [ValidateModel]
    public async Task<Response<AuthorizationResponse>> ExchangeCodeToTokenAsync([FromBody] TokenRequest clientRequest)
    {
        const string uiErrorMessage = "Google authentication failed.";

        Response<AuthorizationResponse> outResponse = new Response<AuthorizationResponse>();

        RestClient client = new RestClient();
        client = client.UseNewtonsoftJson();

        RestRequest request = new RestRequest("https://oauth2.googleapis.com/token", Method.Post);

        request = request.AddJsonBody(
            new ExchangeCodeToTokenRequest
            {
                Code = clientRequest.Code,
                ClientId = this.GoogleProjectConfiguration.ClientId,
                ClientSecret = this.GoogleProjectConfiguration.ClientSecret,
                GrantType = "authorization_code",
                RedirectUri = clientRequest.RedirectUri
            });

        RestResponse<TokenResponse> restResponse = null;

        TokenResponse tokenResponse = null;

        try
        {
            restResponse = await client.ExecuteAsync<TokenResponse>(request);
            tokenResponse = restResponse.Data;
        }
        catch (Exception exception)
        {
            outResponse.ErrorMessage = uiErrorMessage;
            this.LogError(exception, $"Error while getting Google tokens: '{exception.Message}'.");

            await AuditNotifier.ReportExceptionAsync(exception, $"{nameof(GoogleAuthorizationController)}.{nameof(this.ExchangeCodeToTokenAsync)}");
        }
        finally
        {
            client.Dispose();
        }

        if (!outResponse.IsErrored)
        {
            if (!restResponse!.IsSuccessful)
            {
                outResponse.ErrorMessage = uiErrorMessage;
                string logMessage = "Error while getting Google tokens";

                if (tokenResponse?.Error != null)
                {
                    logMessage += $" ({tokenResponse.Error})";
                }

                if (tokenResponse?.ErrorDescription != null)
                {
                    logMessage += $": '{HttpUtility.HtmlDecode(tokenResponse.ErrorDescription).Replace('\n', ' ')}'";
                }

                this.LogError(restResponse.ErrorException, $"{logMessage}.");
            }
            else
            {
                if (tokenResponse!.AccessToken.IsNullOrEmpty())
                {
                    outResponse.ErrorMessage = uiErrorMessage;
                    this.Logger.LogError($"Error while parsing Google tokens: '{TokenResponse.AccessTokenJsonPropertyName} is null'.");
                }
                else if (tokenResponse!.RefreshToken.IsNullOrEmpty())
                {
                    outResponse.ErrorMessage = uiErrorMessage;
                    this.Logger.LogError($"Error while parsing Google tokens: '{TokenResponse.RefreshTokenJsonPropertyName} is null'.");
                }
                else if (tokenResponse!.IdToken.IsNullOrEmpty())
                {
                    outResponse.ErrorMessage = uiErrorMessage;
                    this.Logger.LogError($"Error while parsing Google tokens: '{TokenResponse.IdTokenJsonPropertyName} is null'.");
                }
                else
                {
                    bool decodedSuccessfully = false;

                    List<Claim> tokenClaims = null;

                    try
                    {
                        tokenClaims = new JwtSecurityTokenHandler().ReadJwtToken(tokenResponse.IdToken).Claims.ToList();
                        decodedSuccessfully = true;
                    }
                    catch (Exception exception)
                    {
                        outResponse.ErrorMessage = uiErrorMessage;
                        this.LogError(exception, $"'{TokenResponse.IdTokenJsonPropertyName}' is invalid. Message: '{exception.Message}'.");
                    }

                    if (decodedSuccessfully)
                    {
                        string email;
                        bool? emailVerified;
                        string picture;

                        if (!((email = tokenClaims
                                  .FirstOrDefault(claim => claim.Type == Constants.Identity.ClaimTypes.Email)?.Value) != null &&
                              (emailVerified = tokenClaims
                                  .FirstOrDefault(claim => claim.Type == Constants.Identity.ClaimTypes.EmailVerified)
                                  ?.Value.ToNullableBool()) != null &&
                              (picture = tokenClaims
                                  .FirstOrDefault(claim => claim.Type == Constants.Identity.ClaimTypes.Picture)
                                  ?.Value) != null))
                        {
                            outResponse.ErrorMessage = uiErrorMessage;
                            this.Logger.LogError($"'{TokenResponse.IdTokenJsonPropertyName}' is invalid. Message: ''{Constants.Identity.ClaimTypes.Email}' or '{Constants.Identity.ClaimTypes.EmailVerified}' or '{Constants.Identity.ClaimTypes.Picture}' not found'.");
                        }
                        else if (emailVerified is not true)
                        {
                            outResponse.ErrorMessage = "Email is not verified";
                            this.Logger.LogInformation($"Email '{email}' is not verified.");
                        }
                        else
                        {
                            (bool Succeeded, bool Allowed, UserRole _) result =
                                await this.IamAuthorizationService.IsAllowedUserAsync(email);

                            if (result.Succeeded)
                            {
                                if (result.Allowed)
                                {
                                    SessionData activeSession = await this.SessionStorage.GetAsync(email);

                                    if (activeSession is not null)
                                    {
                                        await this.SessionStorage.DeleteAsync(email);
                                    }

                                    await this.SessionStorage.SaveAsync(
                                        email,
                                        new SessionData(
                                            accessToken: tokenResponse.AccessToken,
                                            idToken: tokenResponse.IdToken,
                                            refreshToken: tokenResponse.RefreshToken));

                                    outResponse.Data = new AuthorizationResponse(tokenResponse.IdToken, picture);
                                }
                                else
                                {
                                    this.Logger.LogInformation($"'{email}' account does not have required permission for this action.");

                                    outResponse.ErrorMessage =
                                        $"'{email}' account does not have required permission for this action.";
                                }
                            }
                            else
                            {
                                // logged earlier
                                outResponse.ErrorMessage =
                                    "GCP IAM service error. Authorization is not completed. Please, try again in a few minutes or call the administrator.";
                            }
                        }
                    }
                }
            }
        }

        return outResponse;
    }

    [HttpGet("close-session")]
    [AllowAnonymous]
    public async Task<Response> CloseSessionAsync()
    {
        Response outResponse = new();

        string email = null;

        if (this.HttpContext.Request.Headers
                .TryGetValue(Constants.Identity.HeaderNames.Request.IdTokenHeaderName, out StringValues idToken) &&
            idToken.Count == 1)
        {
            try
            {
                email = idToken[0].GetTokenClaim(Constants.Identity.ClaimTypes.Email);
            }
            catch (Exception exception)
            {
                this.Logger.LogError(exception, $"Invalid jwt token provided: '{idToken[0]}'.");
            }

            if (!email.IsNullOrEmpty())
            {
                SessionData sessionData = await this.SessionStorage.GetAsync(email);

                if (sessionData is not null)
                {
                    if (sessionData.IdToken == idToken)
                    {
                        this.Logger.LogInformation("Revoking Google tokens and deleting session.");
                        await this.RevokeTokenAsync(sessionData.RefreshToken);
                        await this.SessionStorage.DeleteAsync(this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.Email));
                    }
                    else
                    {
                        this.Logger
                            .LogInformation($"Different '{Constants.Identity.HeaderNames.Request.IdTokenHeaderName}' provided.");
                    }
                }
                else
                {
                    this.Logger.LogInformation($"'{email}' session not found.");
                }
            }
            else
            {
                this.Logger.LogInformation($"{Constants.Identity.ClaimTypes.Email} or {Constants.Identity.ClaimTypes.Expiration} claims not found.");
            }
        }

        return outResponse;
    }
}