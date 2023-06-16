using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Entities;

public class SavedQuery
{
    #region Public Constants

    public const string IdJsonPropertyName = "id";
    public const string NameJsonPropertyName = "name";
    public const string QueryJsonPropertyName = "query";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(IdJsonPropertyName)]
    public long? Id { get; set; }

    [JsonProperty(NameJsonPropertyName)]
    public string Name { get; set; }

    [JsonProperty(QueryJsonPropertyName)]
    public string Query { get; set; }

    #endregion Public Properties
}