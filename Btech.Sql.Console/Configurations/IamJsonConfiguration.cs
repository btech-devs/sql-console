using Newtonsoft.Json;

namespace Btech.Sql.Console.Configurations;

public class IamJsonConfiguration
{
    #region Public Constants

    public const string ClientEmailJsonPropertyName = "client_email";
    public const string PrivateKeyJsonPropertyName = "private_key";
    public const string ProjectIdJsonPropertyName = "project_id";

    #endregion Public Constants

    #region Public Properties

    [JsonProperty(PropertyName = ClientEmailJsonPropertyName)]
    public string ClientEmail { get; set; }

    [JsonProperty(PropertyName = PrivateKeyJsonPropertyName)]
    public string PrivateKey { get; set; }

    [JsonProperty(PropertyName = ProjectIdJsonPropertyName)]
    public string ProjectId { get; set; }

    #endregion Public Properties
}