using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class Column
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string TypeJsonPropertyName = "type";
    public const string MaxLengthJsonPropertyName = "maxLength";
    public const string IsPrimaryKeyJsonPropertyName = "isPrimaryKey";
    public const string IsForeignKeyJsonPropertyName = "isForeignKey";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(TypeJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    [JsonProperty(MaxLengthJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxLength { get; set; }

    [JsonProperty(IsPrimaryKeyJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsPrimaryKey { get; set; }

    [JsonProperty(IsForeignKeyJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsForeignKey { get; set; }

    #endregion Public Properties
}