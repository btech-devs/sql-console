using System.ComponentModel.DataAnnotations;
using Btech.Sql.Console.Enums;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests;

public class ConnectionRequest
{
    #region Public Constants

    public const string HostJsonPropertyName = "host";
    public const string PortJsonPropertyName = "port";
    public const string UsernameJsonPropertyName = "username";
    public const string PasswordJsonPropertyName = "password";
    public const string InstanceTypeJsonPropertyName = "instanceType";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(HostJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    [MaxLength(Constants.HostMaxLength, ErrorMessage = Constants.ValidationErrorMessageTemplates.MaxLength)]
    public string Host { get; set; }

    [JsonProperty(PortJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    [Range(Constants.PortMinValue, Constants.PortMaxValue, ErrorMessage = Constants.ValidationErrorMessageTemplates.Range)]
    public int? Port { get; set; }

    [JsonProperty(UsernameJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    [MaxLength(Constants.UsernameMaxLength, ErrorMessage = Constants.ValidationErrorMessageTemplates.MaxLength)]
    public string Username { get; set; }

    [JsonProperty(PasswordJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    [MaxLength(Constants.PasswordMaxLength, ErrorMessage = Constants.ValidationErrorMessageTemplates.MaxLength)]
    public string Password { get; set; }

    [JsonProperty(InstanceTypeJsonPropertyName)]
    [Required(AllowEmptyStrings = false, ErrorMessage = Constants.ValidationErrorMessageTemplates.Required)]
    public InstanceType InstanceType { get; set; }

    #endregion Public Properties
}