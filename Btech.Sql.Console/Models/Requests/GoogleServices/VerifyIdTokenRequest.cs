using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests.GoogleServices;

public class VerifyIdTokenRequest
{
    #region Public Constants

    public const string IdTokenJsonPropertyName = "id_token";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(IdTokenJsonPropertyName)]
    [Required]
    public string IdToken { get; set; }

    #endregion Public Properties
}