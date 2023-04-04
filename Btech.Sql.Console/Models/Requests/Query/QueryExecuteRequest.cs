using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Requests.Query;

public class QueryExecuteRequest
{
    #region Public Constants

    public const string SqlJsonPropertyName = "sql";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(SqlJsonPropertyName)]
    [Required]
    public string Sql { get; set; }

    #endregion Public Properties
}