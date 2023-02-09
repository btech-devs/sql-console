using Btech.Sql.Console.Extensions;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.GoogleServices;

public class ResponseBase
{
    #region Public Constants

    public const string ErrorJsonPropertyName = "error";
    public const string ErrorDescriptionJsonPropertyName = "error_description";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(ErrorJsonPropertyName)]
    public string Error { get; set; }

    [JsonProperty(ErrorDescriptionJsonPropertyName)]
    public string ErrorDescription { get; set; }

    #endregion Public Properties

    [JsonIgnore]
    public bool IsSucceeded => this.Error.IsNullOrEmpty();
}