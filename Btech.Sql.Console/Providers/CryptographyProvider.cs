using System.Security.Cryptography;
using Btech.Sql.Console.Configurations;
using Microsoft.IdentityModel.Tokens;

namespace Btech.Sql.Console.Providers;

public class CryptographyProvider
{
    public CryptographyProvider(CryptographyConfiguration configuration)
    {
        this.Configuration = configuration;
    }

    private CryptographyConfiguration Configuration { get; }

    private RSA Algorithm { get; } = RSA.Create();

    private string PrepareKey(string inKey)
    {
        return inKey.Replace("\\n", "\n");
    }

    private void ImportPrivateKey()
    {
        this.Algorithm.ImportFromPem(this.PrepareKey(this.Configuration.PrivateKey));
    }

    private void ImportPublicKey()
    {
        this.Algorithm.ImportFromPem(this.PrepareKey(this.Configuration.PublicKey));
    }

    public SigningCredentials GetSigningCredentials()
    {
        this.ImportPrivateKey();

        return new SigningCredentials(new RsaSecurityKey(this.Algorithm), SecurityAlgorithms.RsaSha512);
    }

    public SecurityKey GetPublicSecurityKey()
    {
        this.ImportPublicKey();

        return new RsaSecurityKey(this.Algorithm);
    }
}