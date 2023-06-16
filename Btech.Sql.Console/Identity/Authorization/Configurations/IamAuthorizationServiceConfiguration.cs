namespace Btech.Sql.Console.Identity.Authorization.Configurations;

/// <summary>
/// Represents the configuration required for IAM authorization service.
/// </summary>
public class IamAuthorizationServiceConfiguration
{
    /// <summary>
    /// Gets or sets the service account email used for authentication.
    /// </summary>
    public string ServiceAccountEmail { get; set; }

    /// <summary>
    /// Gets or sets the private key used for authentication.
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    /// Gets or sets the project ID associated with the service account.
    /// </summary>
    public string ProjectId { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated list of granted roles.
    /// </summary>
    public string GrantedRoles { get; set; }
}