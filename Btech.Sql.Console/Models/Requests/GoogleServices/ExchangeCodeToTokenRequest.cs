using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests.GoogleServices;

public class ExchangeCodeToTokenRequest
{
    #region Public Constants

    public const string CodeJsonPropertyName = "code";
    public const string ClientIdJsonPropertyName = "client_id";
    public const string ClientSecretJsonPropertyName = "client_secret";
    public const string GrantTypeJsonPropertyName = "grant_type";
    public const string RedirectUriJsonPropertyName = "redirect_uri";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(CodeJsonPropertyName)]
    [Required]
    public string Code { get; set; }

    [JsonProperty(ClientIdJsonPropertyName)]
    [Required]
    public string ClientId { get; set; }

    [JsonProperty(ClientSecretJsonPropertyName)]
    [Required]
    public string ClientSecret { get; set; }

    [JsonProperty(GrantTypeJsonPropertyName)]
    [Required]
    public string GrantType { get; set; }

    [JsonProperty(RedirectUriJsonPropertyName)]
    [Required]
    public string RedirectUri { get; set; }

    #endregion Public Properties
}