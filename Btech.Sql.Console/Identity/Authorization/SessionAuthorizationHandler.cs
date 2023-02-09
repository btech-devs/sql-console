using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authorization.Base;
using Btech.Sql.Console.Identity.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization;

public class SessionAuthorizationHandler : AuthorizationHandlerBase<SessionAuthorizationRequirement>
{
    public SessionAuthorizationHandler(ILogger<SessionAuthorizationHandler> logger) : base(logger)
    {
    }

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