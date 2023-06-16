using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authorization.Base;
using Btech.Sql.Console.Identity.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization;

/// <summary>
/// An authorization handler for checking session-related requirements.
/// </summary>
public class SessionAuthorizationHandler : AuthorizationHandlerBase<SessionAuthorizationRequirement>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionAuthorizationHandler"/> class with the specified logger.
    /// </summary>
    /// <param name="logger">The logger to be used by the handler.</param>
    public SessionAuthorizationHandler(ILogger<SessionAuthorizationHandler> logger) : base(logger)
    {
    }

    /// <summary>
    /// Checks if the current user has a valid session that meets the specified requirements.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The session authorization requirement.</param>
    /// <returns>A task that represents the asynchronous operation of checking the session requirement.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SessionAuthorizationRequirement requirement)
    {
        string instanceType = context.User.Claims
            .FirstOrDefault(claim => claim.Type == Constants.Identity.ClaimTypes.InstanceType)
            ?.Value;

        string connectionString = context.User.Claims
            .FirstOrDefault(claim => claim.Type == Constants.Identity.ClaimTypes.ConnectionString)
            ?.Value;

        if (!instanceType.IsNullOrEmpty() &&
            !connectionString.IsNullOrEmpty())
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(
                new AuthorizationFailureReason(this, "Unauthorized session."));
        }

        return Task.CompletedTask;
    }
}