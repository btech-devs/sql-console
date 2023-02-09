using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization;

public sealed class GoogleIdentityAuthorizationPolicy : AuthorizationPolicy
{
    public GoogleIdentityAuthorizationPolicy(
        IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes)
        : base(requirements, authenticationSchemes)
    {
    }
}