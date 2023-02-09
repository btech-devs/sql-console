using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.GoogleAuthorization;

public class AuthorizationResponse
{
    #region Public Constants

    public const string IdTokenJsonPropertyName = "idToken";
    public const string PictureUrlJsonPropertyName = "pictureUrl";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(IdTokenJsonPropertyName)]
    public string IdToken { get; }

    [JsonProperty(PictureUrlJsonPropertyName)]
    public string PictureUrl { get; }

    #endregion Public Properties

    public AuthorizationResponse(string idToken, string pictureUrl)
    {
        this.IdToken = idToken;
        this.PictureUrl = pictureUrl;
    }
}