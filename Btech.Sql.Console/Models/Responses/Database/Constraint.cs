using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class Constraint
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string TypeJsonPropertyName = "type";
    public const string SourceTableJsonPropertyName = "sourceTable";
    public const string SourceColumnJsonPropertyName = "sourceColumn";
    public const string TargetTableColumnJsonPropertyName = "targetTable";
    public const string TargetColumnJsonPropertyName = "targetColumn";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(TypeJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty(SourceTableJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string SourceTable { get; set; }

    [JsonProperty(SourceColumnJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string SourceColumn { get; set; }

    [JsonProperty(TargetTableColumnJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string TargetTable { get; set; }

    [JsonProperty(TargetColumnJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string TargetColumn { get; set; }

    #endregion Public Properties
}