using Btech.Sql.Console.Identity.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Btech.Sql.Console.Identity.Authorization;

public class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly AuthorizationPolicy _googleIdentityPolicy;
    private readonly AuthorizationPolicy _sessionPolicy;
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        this._googleIdentityPolicy = this.GetGoogleIdentityPolicy();
        this._sessionPolicy = this.GetSessionPolicy();
        this._fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    private AuthorizationPolicy GetGoogleIdentityPolicy()
    {
        AuthorizationPolicyBuilder policyBuilder = new AuthorizationPolicyBuilder();

        policyBuilder.AddAuthenticationSchemes(Constants.Identity.GoogleIdentityAuthenticationSchemeName);
        policyBuilder.AddRequirements(new GoogleIdentityAuthorizationRequirement());

        return policyBuilder.Build();
    }

    private AuthorizationPolicy GetSessionPolicy()
    {
        AuthorizationPolicyBuilder policyBuilder = new AuthorizationPolicyBuilder();

        policyBuilder.AddAuthenticationSchemes(Constants.Identity.SessionAuthenticationSchemeName);
        policyBuilder.AddRequirements(new SessionAuthorizationRequirement());

        return policyBuilder.Build();
    }

    public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
    {
        return Task.FromResult(
            policyName switch
            {
                Constants.Identity.GoogleIdentityAuthorizationPolicyName => this._googleIdentityPolicy,
                Constants.Identity.SessionAuthorizationPolicyName => this._sessionPolicy,
                _ => throw new ApplicationException("Unsupported authorization policy.")
            });
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => this._fallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => this._fallbackPolicyProvider.GetFallbackPolicyAsync();
}