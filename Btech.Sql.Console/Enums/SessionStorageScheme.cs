namespace Btech.Sql.Console.Enums;

/// <summary>
/// Represents the various session storage schemes that can be used in the application.
/// </summary>
public enum SessionStorageScheme
{
    /// <summary>
    /// Session data will be stored in a local database.
    /// </summary>
    LocalDatabase,

    /// <summary>
    /// Session data will be stored in a static session storage.
    /// </summary>
    StaticSessionStorage,

    /// <summary>
    /// Session data will be stored in a remote database.
    /// </summary>
    RemoteDatabase,

    /// <summary>
    /// Session data will be stored in the Google Cloud Secret Manager.
    /// </summary>
    GoogleCloudSecretManager
}