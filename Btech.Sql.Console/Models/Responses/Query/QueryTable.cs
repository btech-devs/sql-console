using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Query;

public class QueryTable
{
    #region Public Constants

    public const string ColumnsJsonPropertyName = "columns";
    public const string RowsJsonPropertyName = "rows";

    #endregion Public Constants

    #region Pubclid Properties

    [JsonProperty(ColumnsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Column> Columns { get; set; }

    [JsonProperty(RowsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<List<object>> Rows { get; set; }

    #endregion Pubclid Properties
}