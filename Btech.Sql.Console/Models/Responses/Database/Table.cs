﻿using Newtonsoft.Json;

namespace Btech.Sql.Console.Models.Responses.Database;

public class Table
{
    #region Public Constants

    public const string NameJsonPropertyName = "name";
    public const string ColumnsJsonPropertyName = "columns";
    public const string ConstraintsJsonPropertyName = "constraints";
    public const string IndexesJsonPropertyName = "indexes";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(NameJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public string Name { get; set; }

    [JsonProperty(ColumnsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Column> Columns { get; set; }

    [JsonProperty(ConstraintsJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Constraint> Constraints { get; set; }

    [JsonProperty(IndexesJsonPropertyName, NullValueHandling = NullValueHandling.Ignore)]
    public List<Index> Indexes { get; set; }

    #endregion Public Properties
}