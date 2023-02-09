using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.GoogleServices;

public class ParsedIdTokenResponse : ResponseBase
{
    #region Public Constants

    public const string EmailJsonPropertyName = "email";
    public const string EmailVerifiedJsonPropertyName = "email_verified";
    public const string PictureJsonPropertyName = "picture";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(EmailJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Email { get; set; }

    [JsonProperty(EmailVerifiedJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string EmailVerified { get; set; }

    [JsonProperty(PictureJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Picture { get; set; }

    #endregion Public Properties
}