using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class Database
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string TablesJsonPropertyName = "tables";
    public const string PageCountJsonPropertyName = "pageCount";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(TablesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Table> Tables { get; set; }

    [JsonProperty(PageCountJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public int? PageCount { get; set; }

    #endregion Public Properties
}