using System.Security.Cryptography;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace Btech.Sql.Console.Providers;

/// <summary>
/// Provides methods for generating and obtaining cryptographic keys and credentials.
/// </summary>
public class CryptographyProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CryptographyProvider"/> class.
    /// </summary>
    /// <param name="configuration">The configuration settings for the cryptography provider.</param>
    public CryptographyProvider(CryptographyConfiguration configuration)
    {
        this.Configuration = configuration;
    }

    /// <summary>
    /// Gets the cryptography configuration for this instance of the <see cref="CryptographyProvider"/>.
    /// </summary>
    private CryptographyConfiguration Configuration { get; }

    /// <summary>
    /// Gets the RSA algorithm instance used by this instance of the <see cref="CryptographyProvider"/>.
    /// </summary>
    private RSA Algorithm { get; } = RSA.Create();

    /// <summary>
    /// Prepares a key string for import by replacing "\\n" with new line characters.
    /// </summary>
    /// <param name="inKey">The input key string.</param>
    /// <returns>The prepared key string.</returns>
    private string PrepareKey(string inKey)
    {
        return inKey.Replace("\\n", "\n");
    }

    /// <summary>
    /// Imports a key string into the RSA algorithm used by this instance of the <see cref="CryptographyProvider"/>.
    /// </summary>
    /// <param name="key">The key string to import.</param>
    /// <param name="environmentVariableName">The name of the environment variable that contained the key string.</param>
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

    /// <summary>
    /// Gets the signing credentials for this instance of the <see cref="CryptographyProvider"/>.
    /// </summary>
    /// <returns>The signing credentials.</returns>
    public SigningCredentials GetSigningCredentials()
    {
        this.ImportKey(this.Configuration.PrivateKey, Constants.CryptographyPrivateKeyEnvironmentVariableName);

        return new SigningCredentials(new RsaSecurityKey(this.Algorithm), SecurityAlgorithms.RsaSha512);
    }

    /// <summary>
    /// Gets the public security key for this instance of the <see cref="CryptographyProvider"/>.
    /// </summary>
    /// <returns>The public security key.</returns>
    public SecurityKey GetPublicSecurityKey()
    {
        this.ImportKey(this.Configuration.PublicKey, Constants.CryptographyPublicKeyEnvironmentVariableName);

        return new RsaSecurityKey(this.Algorithm);
    }
}