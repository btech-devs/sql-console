namespace Btech.Sql.Console.Configurations;

public class JwtConfiguration
{
    public string Issuer { get; set; }

    public string Audience { get; set; }

    public int SessionTokenLifetimeMinutes { get; set; }

    public int RefreshTokenLifetimeMinutes { get; set; }

    public int? SaltByteSize { get; set; }
}