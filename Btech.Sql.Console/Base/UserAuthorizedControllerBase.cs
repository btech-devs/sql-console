using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Base;

[Authorize(Constants.Identity.GoogleIdentityAuthorizationPolicyName)]
public abstract class UserAuthorizedControllerBase : ControllerBase
{
    protected UserAuthorizedControllerBase(
        ILogger logger, ISessionStorage<SessionData> sessionStorage)
        : base(logger)
    {
        this.SessionStorage = sessionStorage;
    }

    protected ISessionStorage<SessionData> SessionStorage { get; set; }

    protected string GetRequiredUserClaim(string claimType)
    {
        string value;

        if (this.HttpContext.User.Identity?.IsAuthenticated is true)
        {
            value = this.GetUserClaim(claimType);

            if (value.IsNullOrEmpty())
            {
                ApplicationException exception = new ApplicationException($"Required claim was not found: '{claimType}'.");
                this.LogError(exception, "Required claim does not exist.");

                throw exception;
            }
        }
        else
        {
            ApplicationException exception = new ApplicationException("Authentication required.");
            this.LogError(exception, "Can not access claims.");

            throw exception;
        }

        return value;
    }

    protected string GetUserClaim(string claimType)
    {
        return this.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
    }

    protected async Task<SessionData> GetSessionDataAsync()
    {
        SessionData sessionData = await this.SessionStorage
            .GetAsync(this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.Email));

        if (sessionData is null)
        {
            throw new ApplicationException("Session data was not found, but expected.");
        }

        return sessionData;
    }
}