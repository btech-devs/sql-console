using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Btech.Sql.Console.Identity;

public sealed class PolicyEvaluator : IPolicyEvaluator
{
    private readonly IAuthorizationService _authorization;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="authorization">The authorization service.</param>
    public PolicyEvaluator(IAuthorizationService authorization)
    {
        this._authorization = authorization;
    }

    /// <summary>
    /// Add all ClaimsIdentities from an additional ClaimPrincipal to the ClaimsPrincipal
    /// Merges a new claims principal, placing all new identities first, and eliminating
    /// any empty unauthenticated identities from context.User
    /// </summary>
    /// <param name="existingPrincipal">The <see cref="ClaimsPrincipal"/> containing existing <see cref="ClaimsIdentity"/>.</param>
    /// <param name="additionalPrincipal">The <see cref="ClaimsPrincipal"/> containing <see cref="ClaimsIdentity"/> to be added.</param>
    private ClaimsPrincipal MergeUserPrincipal(ClaimsPrincipal existingPrincipal, ClaimsPrincipal additionalPrincipal)
    {
        var newPrincipal = new ClaimsPrincipal();

        // New principal identities go first
        if (additionalPrincipal != null)
        {
            newPrincipal.AddIdentities(additionalPrincipal.Identities);
        }

        // Then add any existing non empty or authenticated identities
        if (existingPrincipal != null)
        {
            newPrincipal.AddIdentities(
                existingPrincipal.Identities
                    .Where(identity => identity.IsAuthenticated || identity.Claims.Any()));
        }

        return newPrincipal;
    }

    /// <summary>
    /// Does authentication for <see cref="AuthorizationPolicy.AuthenticationSchemes"/> and sets the resulting
    /// <see cref="ClaimsPrincipal"/> to <see cref="HttpContext.User"/>.  If no schemes are set, this is a no-op.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns><see cref="AuthenticateResult.Success"/> unless all schemes specified by <see cref="AuthorizationPolicy.AuthenticationSchemes"/> failed to authenticate.  </returns>
    public async Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
    {
        AuthenticateResult result = AuthenticateResult.NoResult();

        if (policy?.AuthenticationSchemes.Any() is true)
        {
            ClaimsPrincipal newPrincipal = null;
            DateTimeOffset? minExpiresUtc = null;

            foreach (string scheme in policy.AuthenticationSchemes
                         .OrderBy(value => value))
            {
                result = await context.AuthenticateAsync(scheme);

                if (result.Succeeded)
                {
                    newPrincipal = this.MergeUserPrincipal(newPrincipal, result.Principal);

                    if (minExpiresUtc is null || result.Properties?.ExpiresUtc < minExpiresUtc)
                    {
                        minExpiresUtc = result.Properties?.ExpiresUtc;
                    }
                }
                else
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                    newPrincipal = null;
                    result = AuthenticateResult.NoResult();

                    break;
                }
            }

            if (newPrincipal != null)
            {
                context.User = newPrincipal;

                AuthenticationTicket ticket =
                    new AuthenticationTicket(newPrincipal, string.Join(";", policy.AuthenticationSchemes))
                    {
                        Properties =
                        {
                            // SignalR will use this property to evaluate auth expiration for long running connections
                            // ExpiresUtc is the easiest property to reason about when dealing with multiple schemes
                            ExpiresUtc = minExpiresUtc
                        }
                    };

                result = AuthenticateResult.Success(ticket);
            }
        }
        else
        {
            // No modifications made to the HttpContext so let's use the existing result if it exists
            result = context.Features.Get<IAuthenticateResultFeature>()
                ?.AuthenticateResult ?? DefaultAuthenticateResult(context);

            static AuthenticateResult DefaultAuthenticateResult(HttpContext context)
            {
                return context.User.Identity?.IsAuthenticated ?? false
                    ? AuthenticateResult.Success(new AuthenticationTicket(context.User, "context.User"))
                    : AuthenticateResult.NoResult();
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts authorization for a policy using <see cref="IAuthorizationService"/>.
    /// </summary>
    /// <param name="policy">The <see cref="AuthorizationPolicy"/>.</param>
    /// <param name="authenticationResult">The result of a call to <see cref="AuthenticateAsync(AuthorizationPolicy, HttpContext)"/>.</param>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <param name="resource">
    /// An optional resource the policy should be checked with.
    /// If a resource is not required for policy evaluation you may pass null as the value.
    /// </param>
    /// <returns>Returns <see cref="PolicyAuthorizationResult.Success"/> if authorization succeeds.
    /// Otherwise returns <see cref="PolicyAuthorizationResult.Forbid(AuthorizationFailure)"/> if <see cref="AuthenticateResult.Succeeded"/>, otherwise
    /// returns  <see cref="PolicyAuthorizationResult.Challenge"/></returns>
    public async Task<PolicyAuthorizationResult> AuthorizeAsync(
        AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object resource)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        PolicyAuthorizationResult policyAuthorizationResult;

        if (authenticationResult.Succeeded)
        {
            AuthorizationResult authorizationResult =
                await this._authorization.AuthorizeAsync(context.User, resource, policy);

            policyAuthorizationResult = authorizationResult.Succeeded
                ? PolicyAuthorizationResult.Success()
                : PolicyAuthorizationResult.Forbid(authorizationResult.Failure);
        }
        else
        {
            policyAuthorizationResult = PolicyAuthorizationResult.Challenge();
        }

        if (policyAuthorizationResult.Forbidden &&
            policyAuthorizationResult.AuthorizationFailure?.FailureReasons.Any() is true)
        {
            context.Response.Headers.Add(
                key: Constants.Identity.HeaderNames.Response.IdentityErrorHeaderName,
                value: policyAuthorizationResult.AuthorizationFailure.FailureReasons.FirstOrDefault()?.Message);
        }

        return policyAuthorizationResult;
    }
}