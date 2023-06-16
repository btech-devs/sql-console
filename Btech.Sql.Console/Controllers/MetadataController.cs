using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Models.Responses.Base;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Controllers;

/// <summary>
/// Controller responsible for handling metadata related requests.
/// </summary>
[Controller]
[Route("api/metadata")]
public class MetadataController
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cryptographyConfiguration">The cryptography configuration.</param>
    /// <param name="googleProjectConfiguration">The Google project configuration.</param>
    public MetadataController(
        ILogger<MetadataController> logger, CryptographyConfiguration cryptographyConfiguration,
        GoogleProjectConfiguration googleProjectConfiguration)
    {
        this.Logger = logger;
        this.CryptographyConfiguration = cryptographyConfiguration;
        this.GoogleProjectConfiguration = googleProjectConfiguration;
    }

    private ILogger Logger { get; }
    private CryptographyConfiguration CryptographyConfiguration { get; }
    private GoogleProjectConfiguration GoogleProjectConfiguration { get; }

    /// <summary>
    /// Gets the client ID.
    /// </summary>
    /// <returns>A response containing the client ID.</returns>
    [HttpGet("client-id")]
    public async Task<Response<string>> GetClientIdAsync()
    {
        Response<string> response = new()
        {
            Data = this.GoogleProjectConfiguration.ClientId
        };

        return await Task.FromResult(response);
    }

    /// <summary>
    /// Gets the JWT public key.
    /// </summary>
    /// <returns>A response containing the JWT public key.</returns>
    [HttpGet("jwt-public-key")]
    public async Task<Response<string>> GetJwtPublicKeyAsync()
    {
        Response<string> response = new()
        {
            Data = this.CryptographyConfiguration.PublicKey
        };

        return await Task.FromResult(response);
    }
}