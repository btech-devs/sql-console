using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Btech.Sql.Console.Extensions;

/// <summary>
/// Contains extension methods for strings.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Gets the value of the specified claim in the given JWT token string.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <param name="claimName">The name of the claim to retrieve.</param>
    /// <returns>The value of the claim, or null if it does not exist.</returns>
    public static string GetTokenClaim(this string token, string claimName)
    {
        return new JwtSecurityToken(token).Claims.FirstOrDefault(claim => claim.Type == claimName)?.Value;
    }

    /// <summary>
    /// Computes the MD5 hash of the specified string.
    /// </summary>
    /// <param name="data">The string to compute the hash of.</param>
    /// <returns>The hexadecimal string representation of the MD5 hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the input string is null or empty.</exception>
    public static string GetMd5(this string data)
    {
        byte[] hashBytes;

        if (data.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(data));
        }

        using (MD5 md5 = MD5.Create())
        {
            hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        return Convert.ToHexString(hashBytes);
    }
}