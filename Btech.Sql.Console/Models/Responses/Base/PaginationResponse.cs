using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Base;

public class PaginationResponse<T> : Response
    where T : class
{
    #region Public Constants

    public const string PageJsonPropertyName = "currentPage";
    public const string PerPageJsonPropertyName = "perPage";
    public const string TotalAmountJsonPropertyName = "totalAmount";
    public const string EntitiesJsonPropertyName = "entities";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(PageJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public int? CurrentPage { get; set; }

    [JsonProperty(PerPageJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public int? PerPage { get; set; }

    [JsonProperty(TotalAmountJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public long? TotalAmount { get; set; }

    [JsonProperty(EntitiesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<T> Entities { get; set; }

    #endregion Public Properties
}