using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests.Query;

public class QueryExecuteDsvRequest : QueryExecuteRequest
{
    #region Public Constants

    public const string SeparatorJsonPropertyName = "separator";
    public const string NewLineJsonPropertyName = "newLine";
    public const string IncludeHeaderJsonPropertyName = "includeHeader";
    public const string AddQuotesJsonPropertyName = "addQuotes";
    public const string NullOutputJsonPropertyName = "nullOutput";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(SeparatorJsonPropertyName)]
    [RegularExpression(",|\t|;", ErrorMessage = "Separator is not allowed. Allowed values are ',' or '\\t' or ';'.")]
    [Required]
    public char Separator { get; set; }

    [JsonProperty(NewLineJsonPropertyName)]
    public string NewLine { get; set; }

    [JsonProperty(IncludeHeaderJsonPropertyName)]
    [Required]
    public bool IncludeHeader { get; set; }

    [JsonProperty(AddQuotesJsonPropertyName)]
    [Required]
    public bool AddQuotes { get; set; }

    [JsonProperty(NullOutputJsonPropertyName)]
    public string NullOutput { get; set; } = string.Empty;

    #endregion Public Properties
}