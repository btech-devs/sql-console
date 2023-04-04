using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Models;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Base;

[Authorize(Constants.Identity.GoogleIdentityAuthorizationPolicyName)]
public abstract class UserAuthorizedControllerBase : ControllerBase
{
    protected UserAuthorizedControllerBase(ILogger logger)
        : base(logger)
    {
    }

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

    protected SessionData GetSessionData()
    {
        if (!this.HttpContext.Items.TryGetValue(Constants.Identity.SessionDataItemName, out var sessionDataObject) ||
            sessionDataObject is not SessionData sessionData)
        {
            throw new ApplicationException("Session data was not found, but expected.");
        }

        return sessionData;
    }

    protected string GetDatabaseHost()
    {
        return this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.Host);
    }

    /// <summary>
    /// This method should not be called in <see cref="Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute"/> methods.
    /// </summary>
    /// <returns><see cref="Constants.Identity.ClaimTypes.Role"/> value.</returns>
    /// <exception cref="ApplicationException">If user role is null.</exception>
    protected UserRole GetUserRole()
    {
        UserRole role = default;

        string roleString = this.GetUserClaim(Constants.Identity.ClaimTypes.Role);

        if (!roleString.IsNullOrEmpty())
        {
            role = Enum.Parse<UserRole>(roleString);
        }

        // for [AllowAnonymous] methods.
        if (role is UserRole.None)
        {
            this.Logger.LogError("Application error. 'UserRole' can not be 'None'.");

            throw new ApplicationException("Application error. Please, call the administrator.");
        }

        return role;
    }
}