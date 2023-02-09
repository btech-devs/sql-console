using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Query;

public class Query
{
    #region Public Constants

    public const string RecordsAffectedJsonPropertyName = "recordsAffected";
    public const string ElapsedTimeMsJsonPropertyName = "elapsedTimeMs";
    public const string TableJsonPropertyName = "table";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(RecordsAffectedJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public int? RecordsAffected { get; set; }

    [JsonProperty(ElapsedTimeMsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public decimal? ElapsedTimeMs { get; set; }

    [JsonProperty(TableJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public QueryTable Table { get; set; }

    #endregion Public Properties
}