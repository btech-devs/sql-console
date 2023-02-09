using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.GoogleServices;

public class TokenResponse : ResponseBase
{
    #region Public Constants

    public const string AccessTokenJsonPropertyName = "access_token";
    public const string RefreshTokenJsonPropertyName = "refresh_token";
    public const string IdTokenJsonPropertyName = "id_token";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(AccessTokenJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string AccessToken { get; set; }

    [JsonProperty(RefreshTokenJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string RefreshToken { get; set; }

    [JsonProperty(IdTokenJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string IdToken { get; set; }

    #endregion Public Properties
}