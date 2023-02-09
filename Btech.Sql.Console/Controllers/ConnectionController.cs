using System.Net.Sockets;
using Btech.Sql.Console.Attributes;
using Btech.Sql.Console.Base;
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

namespace Btech.Sql.Console.Controllers;

[Controller]
[Route("api/connection")]
public class ConnectionController : UserAuthorizedControllerBase
{
    public ConnectionController(
        ILogger<ConnectionController> logger,
        ISessionStorage<SessionData> sessionStorage,
        JwtProvider jwtProvider,
        IConnectorFactory connectorFactory)
        : base(logger, sessionStorage)
    {
        this.JwtProvider = jwtProvider;
        this.ConnectorFactory = connectorFactory;
    }

    private JwtProvider JwtProvider { get; }
    private IConnectorFactory ConnectorFactory { get; }

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
            SessionData sessionData = await this.GetSessionDataAsync();

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

    [HttpGet("close")]
    [Authorize(Constants.Identity.SessionAuthorizationPolicyName)]
    public async Task<Response> CloseAsync()
    {
        Response response = new();

        SessionData sessionData = await this.GetSessionDataAsync();

        sessionData
            .DeleteDbSession(this.Request.Headers[Constants.Identity.HeaderNames.Request.DbSessionTokenHeaderName].ToString());

        await this.SessionStorage.UpdateAsync(
            this.GetRequiredUserClaim(Constants.Identity.ClaimTypes.Email),
            sessionData);

        return response;
    }
}