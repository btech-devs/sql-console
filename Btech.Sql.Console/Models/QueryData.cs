namespace Btech.Sql.Console.Models;

/// <summary>
/// Represents a saved query.
/// </summary>
public class QueryData
{
    /// <summary>
    /// Gets or sets the ID of the saved query.
    /// </summary>
    public long? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the saved query.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the query string of the saved query.
    /// </summary>
    public string Query { get; set; }
}