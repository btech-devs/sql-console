using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization;

public class SessionAuthorizationPolicy : AuthorizationPolicy
{
    public SessionAuthorizationPolicy(
        IEnumerable<IAuthorizationRequirement> requirements, IEnumerable<string> authenticationSchemes)
        : base(requirements, authenticationSchemes)
    {
    }
}