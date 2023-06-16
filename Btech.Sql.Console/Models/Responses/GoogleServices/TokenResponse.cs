using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.GoogleServices;

/// <summary>
/// Represents a response containing token information.
/// </summary>
public class TokenResponse : ResponseBase
{
    #region Public Constants

    /// <summary>
    /// The name of the JSON property that contains the access token.
    /// </summary>
    public const string AccessTokenJsonPropertyName = "access_token";

    /// <summary>
    /// The name of the JSON property that contains the refresh token.
    /// </summary>
    public const string RefreshTokenJsonPropertyName = "refresh_token";

    /// <summary>
    /// The name of the JSON property that contains the ID token.
    /// </summary>
    public const string IdTokenJsonPropertyName = "id_token";

    #endregion Public Constants

    #region Public Properties

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    [JsonProperty(AccessTokenJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    [JsonProperty(RefreshTokenJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string RefreshToken { get; set; }

    /// <summary>
    /// Gets or sets the ID token.
    /// </summary>
    [JsonProperty(IdTokenJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string IdToken { get; set; }

    #endregion Public Properties
}