using System.Text.Encodings.Web;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Btech.Sql.Console.Identity.Authentication.Base;

public abstract class AuthenticationHandlerBase<TOptions> : AuthenticationHandler<TOptions>, IAuthenticationRequestHandler
    where TOptions : AuthenticationSchemeOptions, new()
{
    protected AuthenticationHandlerBase(
        IOptionsMonitor<TOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock,
        ISessionStorage<SessionData> sessionStorage)
        : base(options, logger, encoder, clock)
    {
        this.SessionStorage = sessionStorage;
    }

    protected ISessionStorage<SessionData> SessionStorage { get; set; }

    protected abstract IdentityError IdentityErrorValue { get; }

    protected void AddResponseHeader(string name, string value)
    {
        this.Response.Headers.Add(name, value);
    }

    protected void AddErrorHeader()
    {
        this.AddResponseHeader(Constants.Identity.HeaderNames.Response.IdentityErrorHeaderName, this.IdentityErrorValue.ToString());
    }

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

    public Task<bool> HandleRequestAsync()
    {
        bool handle =
            this.Context.Response.Headers
                .Any(header =>
                    header.Key == Constants.Identity.HeaderNames.Response.IdentityErrorHeaderName);

        return Task.FromResult(handle);
    }
}