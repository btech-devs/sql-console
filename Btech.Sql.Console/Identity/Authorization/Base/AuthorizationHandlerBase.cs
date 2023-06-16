using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Identity.Authorization.Base;

/// <summary>
/// Base class for authorization handlers.
/// </summary>
/// <typeparam name="TRequirement">The type of authorization requirement.</typeparam>
public abstract class AuthorizationHandlerBase<TRequirement> : AuthorizationHandler<TRequirement>
    where TRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationHandlerBase{TRequirement}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    protected AuthorizationHandlerBase(ILogger logger)
    {
        this.Logger = logger;
    }

    /// <summary>
    /// Gets the logger instance used by the authorization handler.
    /// </summary>
    protected ILogger Logger { get; }
}