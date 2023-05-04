using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class Schema
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string TablesJsonPropertyName = "tables";
    public const string RoutinesJsonPropertyName = "routines";
    public const string ViewsJsonPropertyName = "views";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(TablesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Table> Tables { get; set; }

    [JsonProperty(RoutinesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Routine> Routines { get; set; }

    [JsonProperty(ViewsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<View> Views { get; set; }

    #endregion Public Properties
}