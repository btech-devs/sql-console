using System.Security.Cryptography;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Exceptions;
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

    private void ImportKey(string key, string environmentVariableName)
    {
        try
        {
            this.Algorithm.ImportFromPem(this.PrepareKey(key));
        }
        catch (Exception exception)
        {
            throw new EnvironmentVariableException(exception, environmentVariableName);
        }
    }

    public SigningCredentials GetSigningCredentials()
    {
        this.ImportKey(this.Configuration.PrivateKey, Constants.CryptographyPrivateKeyEnvironmentVariableName);

        return new SigningCredentials(new RsaSecurityKey(this.Algorithm), SecurityAlgorithms.RsaSha512);
    }

    public SecurityKey GetPublicSecurityKey()
    {
        this.ImportKey(this.Configuration.PublicKey, Constants.CryptographyPublicKeyEnvironmentVariableName);

        return new RsaSecurityKey(this.Algorithm);
    }
}