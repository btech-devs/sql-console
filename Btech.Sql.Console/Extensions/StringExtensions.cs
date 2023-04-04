using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Btech.Sql.Console.Extensions;

public static class StringExtensions
{
    public static string GetTokenClaim(this string token, string claimName)
    {
        return new JwtSecurityToken(token).Claims.FirstOrDefault(claim => claim.Type == claimName)?.Value;
    }

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