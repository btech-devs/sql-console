using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses;

public class AuthTokenResponse
{
    #region Public Constants

    public const string SessionTokenJsonPropertyName = "sessionToken";
    public const string RefreshTokenJsonPropertyName = "refreshToken";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(SessionTokenJsonPropertyName)]
    public string SessionToken { get; }

    [JsonProperty(RefreshTokenJsonPropertyName)]
    public string RefreshToken { get; }

    #endregion Public Properties

    public AuthTokenResponse(string sessionToken, string refreshToken)
    {
        this.SessionToken = sessionToken;
        this.RefreshToken = refreshToken;
    }
}