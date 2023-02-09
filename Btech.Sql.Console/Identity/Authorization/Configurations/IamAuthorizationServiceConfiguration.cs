namespace Btech.Sql.Console.Identity.Authorization.Configurations;

public class IamAuthorizationServiceConfiguration
{
    public string ServiceAccountEmail { get; set; }

    public string PrivateKey { get; set; }

    public string ProjectId { get; set; }

    public string GrantedRoles { get; set; }
}