using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Models;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Base;

/// <summary>
/// Represents the base class for all controllers that require a Google authentication.
/// </summary>
[Authorize(Constants.Identity.GoogleIdentityAuthorizationPolicyName)]
public abstract class UserAuthorizedControllerBase : ControllerBase
{
    protected UserAuthorizedControllerBase(ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Retrieves the value of the specified user claim from the current request's principal.
    /// If the claim is not present or has an empty value, an exception is thrown.
    /// </summary>
    /// <param name="claimType">The type of the user claim to retrieve.</param>
    /// <returns>The value of the user claim.</returns>
    /// <exception cref="ApplicationException">Thrown if the specified claim is not found or has an empty value, or if authentication is required and the user is not authenticated.</exception>
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

    /// <summary>
    /// Retrieves the value of the specified user claim from the current request's principal.
    /// If the claim is not present or has an empty value, null is returned.
    /// </summary>
    /// <param name="claimType">The type of the user claim to retrieve.</param>
    /// <returns>The value of the user claim, or null if the claim is not present or has an empty value.</returns>
    protected string GetUserClaim(string claimType)
    {
        return this.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
    }

    /// <summary>
    /// Retrieves the <see cref="SessionData"/> object from the current HTTP context's items collection.
    /// If the SessionData object is not found or is not of the expected type, an exception is thrown.
    /// </summary>
    /// <returns>The SessionData object from the current HTTP context's items collection.</returns>
    /// <exception cref="ApplicationException">Thrown if the SessionData object is not found or is not of the expected type.</exception>
    protected SessionData GetSessionData()
    {
        if (!this.HttpContext.Items.TryGetValue(Constants.Identity.SessionDataItemName, out var sessionDataObject) ||
            sessionDataObject is not SessionData sessionData)
        {
            throw new ApplicationException("Session data was not found, but expected.");
        }

        return sessionData;
    }

    /// <summary>
    /// Retrieves the database host from the current request's principal.
    /// </summary>
    /// <returns>The database host retrieved from the current request's principal.</returns>
    /// <exception cref="ApplicationException">Thrown if the required user claim for the database host is not found or has an empty value, or if authentication is required and the user is not authenticated.</exception>
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