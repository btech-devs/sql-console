namespace Btech.Sql.Console.Models;

/// <summary>
/// Represents a database session with the connection string and refresh token.
/// </summary>
public class DbSession
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the database.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the refresh token used to authenticate the session.
    /// </summary>
    public string RefreshToken { get; set; }
}