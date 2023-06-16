using Btech.Sql.Console.Identity.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Btech.Sql.Console.Identity.Authorization;

/// <summary>
/// Provides authorization policies based on the type of authentication scheme and requirements specified.
/// </summary>
public class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly AuthorizationPolicy _googleIdentityPolicy;
    private readonly AuthorizationPolicy _sessionPolicy;
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationPolicyProvider"/> class.
    /// </summary>
    /// <param name="options">The authorization options.</param>
    public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        this._googleIdentityPolicy = this.GetGoogleIdentityPolicy();
        this._sessionPolicy = this.GetSessionPolicy();
        this._fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    /// <summary>
    /// Builds and returns a Google Identity Authorization Policy.
    /// </summary>
    /// <returns>The Google Identity Authorization Policy.</returns>
    private AuthorizationPolicy GetGoogleIdentityPolicy()
    {
        AuthorizationPolicyBuilder policyBuilder = new AuthorizationPolicyBuilder();

        policyBuilder.AddAuthenticationSchemes(Constants.Identity.GoogleIdentityAuthenticationSchemeName);
        policyBuilder.AddRequirements(new GoogleIdentityAuthorizationRequirement());

        return policyBuilder.Build();
    }

    /// <summary>
    /// Builds and returns a Session Authorization Policy.
    /// </summary>
    /// <returns>The Session Authorization Policy.</returns>
    private AuthorizationPolicy GetSessionPolicy()
    {
        AuthorizationPolicyBuilder policyBuilder = new AuthorizationPolicyBuilder();

        policyBuilder.AddAuthenticationSchemes(Constants.Identity.SessionAuthenticationSchemeName);
        policyBuilder.AddRequirements(new SessionAuthorizationRequirement());

        return policyBuilder.Build();
    }

    /// <summary>
    /// Returns a <see cref="Task"/> containing the authorization policy for the specified policy name.
    /// </summary>
    /// <param name="policyName">The name of the policy.</param>
    /// <returns>A <see cref="Task"/> containing the authorization policy for the specified policy name.</returns>
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

    /// <summary>
    /// Returns a <see cref="Task"/> containing the default authorization policy.
    /// </summary>
    /// <returns>A <see cref="Task"/> containing the default authorization policy.</returns>
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => this._fallbackPolicyProvider.GetDefaultPolicyAsync();

    /// <summary>
    /// Returns a <see cref="Task"/> containing the fallback authorization policy.
    /// </summary>
    /// <returns>A <see cref="Task"/> containing the fallback authorization policy.</returns>
    public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => this._fallbackPolicyProvider.GetFallbackPolicyAsync();
}