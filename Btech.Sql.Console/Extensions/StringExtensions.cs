using System.IdentityModel.Tokens.Jwt;

namespace Btech.Sql.Console.Extensions;

public static class StringExtensions
{
    public static string GetTokenClaim(this string token, string claimName)
    {
        return new JwtSecurityToken(token).Claims.FirstOrDefault(claim => claim.Type == claimName)?.Value;
    }
}