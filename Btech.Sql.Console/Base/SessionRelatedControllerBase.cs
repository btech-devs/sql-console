using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Btech.Sql.Console.Base;

[Authorize(Constants.Identity.SessionAuthorizationPolicyName)]
public abstract class SessionRelatedControllerBase : UserAuthorizedControllerBase
{
    protected SessionRelatedControllerBase(
        ILogger logger, ISessionStorage<SessionData> sessionStorage) : base(logger, sessionStorage)
    {
    }

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