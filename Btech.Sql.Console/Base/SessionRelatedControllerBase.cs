using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Identity.Authentication;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Base;

/// <summary>
/// Represents the base class for all controllers that require a session and Google authentication.
/// </summary>
[Authorize(Constants.Identity.SessionAuthorizationPolicyName)]
public abstract class SessionRelatedControllerBase : UserAuthorizedControllerBase
{
    protected SessionRelatedControllerBase(
        ILogger logger)
        : base(logger)
    {
    }

    /// <summary>
    /// Retrieves the instance type from the current request's principal.
    /// </summary>
    /// <returns>The instance type retrieved from the current request's principal.</returns>
    /// <exception cref="ArgumentException">Thrown if the instance type claim is not found, has an empty value, or cannot be parsed into an InstanceType enum value.</exception>
    protected InstanceType GetInstanceType()
    {
        string rawInstanceType = this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.InstanceType);

        if (rawInstanceType.IsNullOrEmpty() || !Enum.TryParse(rawInstanceType, out InstanceType instanceType))
        {
            this.Logger?.LogError($"Instance credentials are not found in claims: '{Constants.Identity.ClaimTypes.InstanceType}'.");

            // TODO: nothing happens on the console page ???
            throw new ArgumentException($"Instance credentials are not found in claims: '{Constants.Identity.ClaimTypes.InstanceType}'.");
        }

        return instanceType;
    }

    /// <summary>
    /// Retrieves the connection string from the current request's principal that included at <see cref="SessionAuthenticationHandler"/>.
    /// </summary>
    /// <returns>The instance type retrieved from the current request's principal.</returns>
    /// <exception cref="ArgumentException">Thrown if the instance type claim is not found, has an empty value, or cannot be parsed into an InstanceType enum value.</exception>
    protected string GetConnectionString(string database = null)
    {
        string connectionString = this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.ConnectionString);

        if (connectionString.IsNullOrEmpty())
        {
            this.Logger?.LogError($"Instance credentials are not found in claims: '{Constants.Identity.ClaimTypes.ConnectionString}'.");

            // TODO: nothing happens on the console page ???
            throw new ArgumentException($"Instance credentials are not found in claims: '{Constants.Identity.ClaimTypes.ConnectionString}'.");
        }

        if (!database.IsNullOrEmpty())
        {
            connectionString = ConnectionStringBuilder
                .SetDatabase(connectionString, database);
        }

        return connectionString;
    }
}