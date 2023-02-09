using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests.GoogleServices;

public class RefreshTokenRequest
{
    #region Public Constants

    public const string ClientIdJsonPropertyName = "client_id";
    public const string ClientSecretJsonPropertyName = "client_secret";
    public const string RefreshTokenJsonPropertyName = "refresh_token";
    public const string GrantTypeJsonPropertyName = "grant_type";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(ClientIdJsonPropertyName)]
    [Required]
    public string ClientId { get; set; }

    [JsonProperty(ClientSecretJsonPropertyName)]
    [Required]
    public string ClientSecret { get; set; }

    [JsonProperty(RefreshTokenJsonPropertyName)]
    [Required]
    public string RefreshToken { get; set; }

    [JsonProperty(GrantTypeJsonPropertyName)]
    [Required]
    public string GrantType { get; set; }

    #endregion Public Properties
}