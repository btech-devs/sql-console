using Btech.Sql.Console.Extensions;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Base;

public class Response
{
    #region Public Constants

    public const string ErrorMessageJsonPropertyName = "errorMessage";
    public const string ErrorMessagesJsonPropertyName = "errorMessages";
    public const string ValidationErrorMessagesJsonPropertyName = "validationErrorMessages";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(ErrorMessageJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string ErrorMessage { get; set; }

    [JsonProperty(ErrorMessagesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<string> ErrorMessages { get; set; }

    [JsonProperty(ValidationErrorMessagesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, string> ValidationErrorMessages { get; set; }

    [JsonIgnore]
    public bool IsErrored => !this.ErrorMessage.IsNullOrEmpty() ||
                             this.ErrorMessages?.Any() is true ||
                             this.ValidationErrorMessages?.Any() is true;

    #endregion Public Properties
}

public class Response<T> : Response
{
    public const string DataJsonPropertyName = "data";

    [JsonProperty(DataJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public T Data { get; set; }
}