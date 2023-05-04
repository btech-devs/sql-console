using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class Index
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    #endregion Public Properties
}