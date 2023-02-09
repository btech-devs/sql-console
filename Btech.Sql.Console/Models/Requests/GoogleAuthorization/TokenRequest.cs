using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests.GoogleAuthorization;

public class TokenRequest
{
    #region Public Constants

    public const string CodeJsonPropertyName = "code";
    public const string RedirectUriJsonPropertyName = "redirect_uri";

    #endregion Public Constantsі

    #region Public Properties

    [JsonProperty(PropertyName = CodeJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    public string Code { get; set; }

    [JsonProperty(PropertyName = RedirectUriJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    public string RedirectUri { get; set; }

    #endregion Public Properties
}