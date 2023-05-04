using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class View
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string ColumnsJsonPropertyName = "columns";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(ColumnsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Column> Columns { get; set; }

    #endregion Public Properties
}