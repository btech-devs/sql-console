using System.Net.Sockets;
using Btech.Sql.Console.Attributes;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Requests;
using Btech.Sql.Console.Models.Responses;
using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Providers;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Npgsql;
using ServiceCollectionExtensions = Btech.Sql.Console.Extensions.ServiceCollectionExtensions;

namespace Btech.Sql.Console.Controllers;

/// <summary>
/// Represents a controller that handles requests related to database connections, and requires user authorization.
/// </summary>
[Controller]
[Route("api/connection")]
public class ConnectionController : UserAuthorizedControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionController"/> class with the specified dependencies.
    /// </summary>
    /// <param name="logger">The logger used for logging.</param>
    /// <param name="jwtProvider">The JWT provider used for generating and validating JSON Web Tokens.</param>
    /// <param name="connectorFactory">The factory used for creating instances of connectors for connecting to database instances.</param>
    /// <param name="sessionStorage">The storage used for persisting session data.</param>
    public ConnectionController(
        ILogger<ConnectionController> logger,
        JwtProvider jwtProvider,
        IConnectorFactory connectorFactory,
        ISessionStorage<SessionData> sessionStorage)
        : base(logger)
    {
        this.JwtProvider = jwtProvider;
        this.ConnectorFactory = connectorFactory;
        this.SessionStorage = sessionStorage;
    }

    private ISessionStorage<SessionData> SessionStorage { get; }
    private JwtProvider JwtProvider { get; }
    private IConnectorFactory ConnectorFactory { get; }

    /// <summary>
    /// Opens a new connection to the database instance specified in the <paramref name="connectionRequest"/> parameter, using the credentials specified in the request.
    /// </summary>
    /// <param name="connectionRequest">The request containing the credentials to use when opening the connection.</param>
    /// <returns>A response object containing an authentication token response, if the connection was successfully opened, or an error message, if the connection failed.</returns>
    [HttpPost("open")]
    [ValidateModel]
    public async Task<Response<AuthTokenResponse>> OpenAsync([FromBody] ConnectionRequest connectionRequest)
    {
        Response<AuthTokenResponse> response = new Response<AuthTokenResponse>();

        bool isValidCredentials = false;

        string connectionString = ConnectionStringBuilder
            .CreateConnectionString(
                connectionRequest.InstanceType,
                connectionRequest.Host,
                connectionRequest.Port!.Value,
                connectionRequest.Username,
                connectionRequest.Password);

        ConnectorBase connector = null;

        try
        {
            connector = this.ConnectorFactory
                .Create(connectionRequest.InstanceType, connectionString);

            await connector.OpenConnectionAsync();

            isValidCredentials = true;

            await AuditNotifier.IncreaseConnectionNumberAsync();
        }
        catch (SocketException socketException)
        {
            response.ErrorMessage = socketException.Message;
            this.Logger.LogInformation(socketException.Message);
        }
        catch (SqlException sqlException)
        {
            response.ErrorMessage = sqlException.Message;
            this.Logger.LogInformation(sqlException.Message);
        }
        catch (PostgresException postgresException)
        {
            response.ErrorMessage = postgresException.Message;
            this.Logger.LogInformation(postgresException.Message);
        }
        catch (NpgsqlException npgsqlException)
        {
            response.ErrorMessage = npgsqlException.Message;

            if (npgsqlException.InnerException?.Message is not null)
            {
                response.ErrorMessage += $": {npgsqlException.InnerException?.Message}";
            }

            this.Logger.LogInformation(response.ErrorMessage);
        }
        finally
        {
            if (connector != null)
            {
                await connector.DisposeAsync();
            }
        }

        if (isValidCredentials)
        {
            SessionData sessionData = this.GetSessionData();

            string sessionToken = this.JwtProvider.CreateSessionToken(
                instanceType: connectionRequest.InstanceType.ToString(),
                host: connectionRequest.Host);

            string refreshToken = this.JwtProvider.CreateRefreshToken();

            if (sessionData.CreateDbSession(sessionToken, refreshToken, connectionString) &&
                await this.SessionStorage
                    .UpdateAsync(this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.Email), sessionData))
            {
                response.Data = new AuthTokenResponse(sessionToken, refreshToken);
            }
        }

        return response;
    }

    /// <summary>
    /// Endpoint for closing a database connection by deleting the database session from the session storage.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [HttpGet("close")]
    [Authorize(Constants.Identity.SessionAuthorizationPolicyName)]
    public async Task<Response> CloseAsync()
    {
        Response response = new();

        SessionData sessionData = this.GetSessionData();

        sessionData
            .DeleteDbSession(this.Request.Headers[Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName].ToString());

        await this.SessionStorage.UpdateAsync(
            this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.Email),
            sessionData);

        return response;
    }

    /// <summary>
    /// Retrieves a static connection token for the application, based on environment variables and the current session storage scheme.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a response object containing the connection token data.</returns>
    [HttpGet("static-connection")]
    public Task<Response<AuthTokenResponse>> GetStaticConnectionAsync()
    {
        Response<AuthTokenResponse> response = new Response<AuthTokenResponse>();

        if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.StaticSessionStorage)
        {
            string host = Environment.GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.Host);
            string instanceType = Environment.GetEnvironmentVariable(Constants.Identity.StaticConnectionEnvironmentVariables.InstanceType);

            string sessionToken = this.JwtProvider.CreateSessionToken(
                instanceType: instanceType,
                host: host);

            string refreshToken = this.JwtProvider.CreateRefreshToken();

            response.Data = new AuthTokenResponse(sessionToken, refreshToken);
        }

        return Task.FromResult(response);
    }
}