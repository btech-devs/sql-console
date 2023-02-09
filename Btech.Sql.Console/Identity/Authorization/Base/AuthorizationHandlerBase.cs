using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization.Base;

public abstract class AuthorizationHandlerBase<TRequirement> : AuthorizationHandler<TRequirement>
    where TRequirement : IAuthorizationRequirement
{
    protected AuthorizationHandlerBase(ILogger logger)
    {
        this.Logger = logger;
    }

    protected ILogger Logger { get; }
}