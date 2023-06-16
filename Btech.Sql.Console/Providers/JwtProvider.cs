using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Utils;
using Microsoft.IdentityModel.Tokens;

namespace Btech.Sql.Console.Providers;

/// <summary>
/// Provides methods for creating and validating JSON Web Tokens (JWT).
/// </summary>
public class JwtProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JwtProvider"/> class with the specified dependencies.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="jwtConfiguration">The JWT configuration.</param>
    /// <param name="cryptographyProvider">The cryptography provider.</param>
    public JwtProvider(ILogger<JwtProvider> logger, JwtConfiguration jwtConfiguration, CryptographyProvider cryptographyProvider)
    {
        this.Logger = logger;
        this.Configuration = jwtConfiguration;
        this.CryptographyProvider = cryptographyProvider;
    }

    private ILogger Logger { get; }
    private JwtConfiguration Configuration { get; }
    private CryptographyProvider CryptographyProvider { get; }

    /// <summary>
    /// Generates a random salt value.
    /// </summary>
    /// <returns>The salt value as a string.</returns>
    private string GenerateSalt()
    {
        byte[] randomBytes = null;

        if (this.Configuration.SaltByteSize.HasValue)
        {
            randomBytes = new Byte[this.Configuration.SaltByteSize.Value];
            RandomNumberGenerator.Create().GetNonZeroBytes(randomBytes);
        }

        return randomBytes?.Any() is true
            ? Base64UrlEncoder.Encode(randomBytes)
            : null;
    }

    /// <summary>
    /// Creates a new JWT token with the specified expiration time and claims.
    /// </summary>
    /// <param name="expiration">The token expiration time in minutes.</param>
    /// <param name="claims">The list of claims to include in the token.</param>
    /// <returns>The generated JWT token as a string.</returns>
    private string CreateToken(int expiration, List<Claim> claims = null)
    {
        JwtSecurityTokenHandler tokenHandler = new();

        string salt;

        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Expires = DateTime.UtcNow.AddMinutes(expiration),
            SigningCredentials = this.CryptographyProvider.GetSigningCredentials(),
            Issuer = this.Configuration.Issuer,
            IssuedAt = DateTime.UtcNow,
            Audience = this.Configuration.Audience,
            AdditionalInnerHeaderClaims = (salt = this.GenerateSalt()).IsNullOrEmpty()
                ? null
                : new Dictionary<string, object>
                {
                    { "salt", salt }
                }
        };

        if (claims?.Any() is true)
        {
            tokenDescriptor.Subject ??= new ClaimsIdentity();
            tokenDescriptor.Subject?.AddClaims(claims);
        }

        return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
    }

    /// <summary>
    /// Creates a new session token with the specified instance type and host values.
    /// </summary>
    /// <param name="instanceType">The instance type to include in the token.</param>
    /// <param name="host">The host value to include in the token.</param>
    /// <returns>The generated session token as a string.</returns>
    public string CreateSessionToken(string instanceType, string host)
    {
        return this.CreateToken(
            this.Configuration.SessionTokenLifetimeMinutes,
            new List<Claim>
            {
                new(Constants.Identity.ClaimTypes.InstanceType, instanceType),
                new(Constants.Identity.ClaimTypes.Host, host)
            });
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <returns>The generated refresh token as a string.</returns>
    public string CreateRefreshToken()
    {
        return this.CreateToken(this.Configuration.RefreshTokenLifetimeMinutes);
    }

    /// <summary>
    /// Determines if a given JWT token is valid.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param  name="tokenExpired">A flag indicating if the token has expired.</param>
    /// <returns>A flag indicating if the token is valid.</returns>
    public bool IsValidToken(string token, out bool tokenExpired)
    {
        bool isValid = false;
        tokenExpired = false;

        if (!token.IsNullOrEmpty())
        {
            JwtSecurityTokenHandler tokenHandler = new();

            try
            {
                tokenHandler.ValidateToken(
                    token: token,
                    validationParameters: new()
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = this.CryptographyProvider.GetPublicSecurityKey(),
                        ValidIssuer = this.Configuration.Issuer,
                        ValidateAudience = true,
                        ValidAudience = this.Configuration.Audience,
                        // set ClockSkew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                        ClockSkew = TimeSpan.Zero
                    },
                    validatedToken: out SecurityToken _);

                isValid = true;
            }
            catch (SecurityTokenExpiredException expiredException)
            {
                this.Logger.LogInformation(expiredException.Message);
                isValid = true;
                tokenExpired = true;
            }
            catch (SecurityTokenInvalidSignatureException signatureException)
            {
                this.Logger.LogWarning(signatureException.Message);
            }
            catch (Exception otherUnexpectedException)
            {
                this.Logger.LogError(otherUnexpectedException.Message);

                AuditNotifier.ReportExceptionAsync(otherUnexpectedException, $"{nameof(JwtProvider)}.{nameof(this.IsValidToken)}").Wait();
            }
        }

        return isValid;
    }
}