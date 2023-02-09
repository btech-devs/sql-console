using Btech.Sql.Console.Configurations;
using Btech.Sql.Console.Models.Responses.Base;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Controllers;

[Controller]
[Route("api/metadata")]
public class MetadataController
{
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

    [HttpGet("client-id")]
    public async Task<Response<string>> GetClientIdAsync()
    {
        Response<string> response = new()
        {
            Data = this.GoogleProjectConfiguration.ClientId
        };

        return await Task.FromResult(response);
    }

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