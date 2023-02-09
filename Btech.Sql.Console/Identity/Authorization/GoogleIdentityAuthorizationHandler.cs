using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authorization.Base;
using Btech.Sql.Console.Identity.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization;

public class GoogleIdentityAuthorizationHandler : AuthorizationHandlerBase<GoogleIdentityAuthorizationRequirement>
{
    public GoogleIdentityAuthorizationHandler(
        ILogger<GoogleIdentityAuthorizationHandler> logger, IamAuthorizationService iamService)
        : base(logger)
    {
        this.IamService = iamService;
    }

    private IamAuthorizationService IamService { get; }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, GoogleIdentityAuthorizationRequirement requirement)
    {
        string email = context.User.Claims
            .FirstOrDefault(claim => claim.Type == Constants.Identity.ClaimTypes.Email)
            ?.Value;

        if (!email.IsNullOrEmpty())
        {
            (bool Succeeded, bool Allowed) result = await this.IamService.IsAllowedUserAsync(email);

            if (result.Succeeded)
            {
                if (result.Allowed)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    this.Logger.LogInformation($"'{email}' account does not have required permission for this action.");

                    context.Fail(
                        new AuthorizationFailureReason(this, $"'{email}' account does not have required permission for this action."));
                }
            }
            else
            {
                // logged earlier
                context.Fail(
                    new AuthorizationFailureReason(this, "'GoogleIAM' service error. Authorization is not complete. Please, call the administrator."));
            }
        }
    }
}