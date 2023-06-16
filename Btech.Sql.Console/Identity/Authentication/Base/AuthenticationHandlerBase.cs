using System.Text.Encodings.Web;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Btech.Sql.Console.Identity.Authentication.Base;

/// <summary>
/// Base class for authentication handlers, implementing the IAuthenticationRequestHandler interface.
/// </summary>
/// <typeparam name="TOptions">The authentication scheme options.</typeparam>
public abstract class AuthenticationHandlerBase<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationRequestHandler
    where TOptions : AuthenticationSchemeOptions, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationHandlerBase{TOptions}"/> class.
    /// </summary>
    /// <param name="options">The authentication scheme options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="encoder">The URL encoder.</param>
    /// <param name="clock">The system clock.</param>
    /// <param name="sessionStorage">The session storage.</param>
    protected AuthenticationHandlerBase(
        IOptionsMonitor<TOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        ISessionStorage<SessionData> sessionStorage)
        : base(options, logger, encoder, clock)
    {
        this.SessionStorage = sessionStorage;
    }

    /// <summary>
    /// Gets the session storage.
    /// </summary>
    protected ISessionStorage<SessionData> SessionStorage { get; set; }

    /// <summary>
    /// Gets the identity error value.
    /// </summary>
    protected abstract IdentityError IdentityErrorValue { get; }

    /// <summary>
    /// Adds a response header.
    /// </summary>
    /// <param name="name">The name of the header.</param>
    /// <param name="value">The value of the header.</param>
    protected void AddResponseHeader(string name, string value)
    {
        this.Response.Headers.Add(name, value);
    }

    /// <summary>
    /// Adds an error header.
    /// </summary>
    protected void AddErrorHeader()
    {
        this.AddResponseHeader(Constants.Identity.HeaderNames.Response.IdentityErrorHeaderName, this.IdentityErrorValue.ToString());
    }

    /// <summary>
    /// Gets a request header by name.
    /// </summary>
    /// <param name="headerName">The name of the header to retrieve.</param>
    /// <returns>The value of the header.</returns>
    protected string GetRequestHeader(string headerName)
    {
        string header = null;

        if (this.Request.Headers.TryGetValue(headerName, out StringValues values) &&
            values.Count == 1)
        {
            header = values[0];
        }
        else if (values.Count > 1)
        {
            this.Logger.LogError($"More than 1 header with name '{headerName}' found.");

            throw new ApplicationException($"More than 1 header with name '{headerName}' found.");
        }

        return header;
    }

    /// <summary>
    /// Tries to get a claim from a JWT token.
    /// </summary>
    /// <param name="token">The JWT token.</param>
    /// <param name="claimName">The name of the claim to retrieve.</param>
    /// <param name="claimValue">The value of the claim, if it exists.</param>
    /// <returns><c>true</c> if the JWT is valid and the claim exists; otherwise, <c>false</c>.</returns>
    protected bool TryGetTokenClaim(string token, string claimName, out string claimValue)
    {
        bool isValidJwt = true;

        try
        {
            claimValue = token.GetTokenClaim(claimName);
        }
        catch (Exception exception)
        {
            isValidJwt = false;
            claimValue = null;
            this.Logger.LogError(exception, $"Invalid jwt token provided: '{token}'.");
        }

        return isValidJwt;
    }

    /// <summary>
    /// Handles the authentication request asynchronously by checking if the context response headers contain an identity error header.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating if an identity error header was found in the response headers.</returns>
    public Task<bool> HandleRequestAsync()
    {
        bool handle =
            this.Context.Response.Headers
                .Any(
                    header =>
                        header.Key == Constants.Identity.HeaderNames.Response.IdentityErrorHeaderName);

        return Task.FromResult(handle);
    }
}