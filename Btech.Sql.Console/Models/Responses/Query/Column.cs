using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Query;

public class Column
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string TypeJsonPropertyName = "type";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(TypeJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Type { get; set; }

    #endregion Public Properties
}