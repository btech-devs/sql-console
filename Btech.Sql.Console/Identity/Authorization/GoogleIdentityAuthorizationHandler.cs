using System.Security.Claims;
using Btech.Sql.Console.Enums;
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
            (bool Succeeded, bool Allowed, UserRole Role) result = await this.IamService.IsAllowedUserAsync(email);

            if (result.Succeeded)
            {
                if (result.Allowed)
                {
                    if (result.Role is not UserRole.None)
                    {
                        context.User.AddIdentity(
                            new ClaimsIdentity(
                                new List<Claim>
                                {
                                    new(Constants.Identity.ClaimTypes.Role, result.Role.ToString()!)
                                }));

                        this.Logger.LogInformation($"'{email}' authorized as '{result.Role.ToString()!}'.");
                        context.Succeed(requirement);
                    }
                    else
                    {
                        this.Logger.LogInformation($"'{email}' have no role. Access denied.");

                        context.Fail(
                            new AuthorizationFailureReason(this, $"'{email}' have no role. Access denied."));
                    }
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
                    new AuthorizationFailureReason(this, "GCP IAM service error. Authorization is not completed. Please, call the administrator."));
            }
        }
    }
}