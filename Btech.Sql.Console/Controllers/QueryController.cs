using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Data.Common;
using Btech.Sql.Console.Attributes;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Factories;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models.Requests.Query;
using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Models.Responses.Connector;
using Btech.Sql.Console.Models.Responses.Query;
using Btech.Sql.Console.Utils;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Controllers;

/// <summary>
/// Controller for handling SQL queries.
/// </summary>
[Controller]
[Route("api/query")]
public class QueryController : SessionRelatedControllerBase
{
    public QueryController(
        ILogger<QueryController> logger, IConnectorFactory connectorFactory)
        : base(logger)
    {
        this.ConnectorFactory = connectorFactory;
    }

    private IConnectorFactory ConnectorFactory { get; }

    /// <summary>
    /// Executes a SQL query against the specified database.
    /// </summary>
    /// <param name="database">The name of the database to execute the query against.</param>
    /// <param name="queryExecuteRequest">The request object containing the SQL query to execute.</param>
    /// <returns>A response object containing the results of the query execution.</returns>
    [HttpPost("execute/{database}")]
    [ValidateModel]
    public async Task<Response<Query>> ExecuteAsync([FromRoute] string database, [FromBody] QueryExecuteRequest queryExecuteRequest)
    {
        Response<Query> queryResponse = new();

        if (!PermissionRuleFactory
                .IsAllowedDatabase(database, this.GetUserRole(), this.GetDatabaseHost()))
        {
            queryResponse.ErrorMessage = "You do not have permission for this request.";
        }
        else
        {
            await using (ConnectorBase connector = this.ConnectorFactory
                             .Create(this.GetInstanceType(), this.GetConnectionString(database)))
            {
                Query data = new Query();

                await connector.OpenConnectionAsync();

                DataTable dataTable = new DataTable();
                ReadOnlyCollection<DbColumn> columnsSchema;

                await using (DbCommand query = connector.CreateCommand(queryExecuteRequest.Sql))
                {
                    DateTime start = DateTime.UtcNow;

                    await using (DbDataReader reader = await query.ExecuteReaderAsync())
                    {
                        columnsSchema = await reader.GetColumnSchemaAsync();
                        dataTable.Load(reader);
                        DateTime end = DateTime.UtcNow;
                        data.RecordsAffected = reader.RecordsAffected;
                        data.ElapsedTimeMs = (end - start).ToElapsedTimeMs();
                    }
                }

                if (columnsSchema.Any())
                {
                    data.Table = new()
                    {
                        Columns = new(),
                        Rows = new()
                    };

                    this.LogDebug("Start generating response.");

                    foreach (DbColumn dataTableColumn in columnsSchema)
                    {
                        data.Table.Columns.Add(
                            new Column
                            {
                                Name = dataTableColumn.ColumnName,
                                Type = dataTableColumn.DataTypeName
                            });
                    }

                    foreach (DataRow dataTableRow in dataTable.Rows)
                        data.Table.Rows.Add(dataTableRow.ItemArray.ToList());
                }

                queryResponse.Data = data;
            }
        }

        return queryResponse;
    }

    /// <summary>
    /// Executes a SQL query on the specified database and returns the result as a delimited separated values (DSV) file.
    /// </summary>
    /// <param name="database">The name of the database to execute the query on.</param>
    /// <param name="queryExecuteDsvRequest">The request containing the SQL query and DSV formatting options.</param>
    [HttpPost("execute/{database}/dsv")]
    [ValidateModel]
    public async Task ExecuteDsvAsync([FromRoute] string database, [FromBody] QueryExecuteDsvRequest queryExecuteDsvRequest)
    {
        try
        {
            this.HttpContext.Features.Get<IHttpResponseBodyFeature>()!.DisableBuffering();

            if (!PermissionRuleFactory
                    .IsAllowedDatabase(database, this.GetUserRole(), this.GetDatabaseHost()))
            {
                this.Response.StatusCode = 403;
            }
            else
            {
                this.HttpContext.Features.Get<IHttpResponseBodyFeature>()!.DisableBuffering();

                await using (ConnectorBase connector = this.ConnectorFactory
                                 .Create(this.GetInstanceType(), this.GetConnectionString(database)))
                {
                    await connector.OpenConnectionAsync();

                    await using (DbCommand query = connector.CreateCommand(queryExecuteDsvRequest.Sql))
                    {
                        await using (DbDataReader reader = await query.ExecuteReaderAsync())
                        {
                            this.Response.ContentType = queryExecuteDsvRequest.Separator switch
                            {
                                ',' => "text/csv",
                                '\t' => "text/tab-separated-values",
                                ';' => "text/csv",
                                _ => "text/plain"
                            };

                            bool isFirstRow = true;

                            while (await reader.ReadAsync())
                            {
                                List<string> columnList = new List<string>();
                                List<string> headerList = new List<string>();

                                for (int columnIndex = 0; columnIndex < reader.FieldCount; ++columnIndex)
                                {
                                    string value;

                                    if (isFirstRow && queryExecuteDsvRequest.IncludeHeader)
                                    {
                                        value = reader.GetName(columnIndex);

                                        if (queryExecuteDsvRequest.AddQuotes)
                                            value = $"\"{value}\"";

                                        headerList.Add(value);
                                    }

                                    Type type = reader.GetFieldType(columnIndex);

                                    if (type == typeof(byte[]))
                                    {
                                        value = Convert.ToBase64String((byte[]) reader[columnIndex]);

                                        // Check for zipped content
                                        if (!value.StartsWith("H4sI"))
                                        {
                                            value = Encoding.UTF8.GetString((byte[]) reader[columnIndex]);
                                        }
                                    }
                                    else
                                        value = reader[columnIndex].ToString();

                                    value ??= queryExecuteDsvRequest.NullOutput;

                                    columnList.Add(value);
                                }

                                if (queryExecuteDsvRequest.AddQuotes)
                                    columnList = columnList.Select(value => $"\"{value}\"").ToList();

                                if (isFirstRow && queryExecuteDsvRequest.IncludeHeader)
                                {
                                    await this.Response.BodyWriter
                                        .WriteAsync(Encoding.UTF8.GetBytes(string.Join(queryExecuteDsvRequest.Separator, headerList)));

                                    await this.Response.BodyWriter
                                        .WriteAsync(Encoding.UTF8.GetBytes(queryExecuteDsvRequest.NewLine));
                                }

                                if (!isFirstRow)
                                    await this.Response.BodyWriter
                                        .WriteAsync(Encoding.UTF8.GetBytes(queryExecuteDsvRequest.NewLine));

                                isFirstRow = false;

                                await this.Response.BodyWriter
                                    .WriteAsync(Encoding.UTF8.GetBytes(string.Join(queryExecuteDsvRequest.Separator, columnList)));

                                await this.Response.BodyWriter.FlushAsync();
                            }
                        }
                    }
                }
            }
        }
        catch (DbException exception)
        {
            this.LogDebug(exception.Message, exception);

            this.Response.StatusCode = 400;

            await this.Response.WriteAsJsonAsync(
                new Response
                {
                    ErrorMessage = exception.Message
                });
        }

        this.LogDebug("Close connection.");
    }

    /// <summary>
    /// Imports an SQL file to the specified database.
    /// </summary>
    /// <param name="database">The name of the database to import the file to.</param>
    /// <param name="file">The SQL file to import.</param>
    /// <returns>A response containing information about the imported data.</returns>
    [HttpPost("import/{database}/sql"), DisableRequestSizeLimit]
    public async Task<Response<Query>> ImportSqlAsync(
        [FromRoute] string database,
        [FromForm] IFormFile file)
    {
        const short timeLimit = 180;

        Response<Query> response = new();

        string fileExtension = file.FileName.Split('.').Last();

        string[] allowedExtensions = { "sql", "pgsql" };

        if (!allowedExtensions.Contains(fileExtension))
        {
            this.Response.StatusCode = 400;
        }
        else
        {
            await using (Stream stream = file.OpenReadStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    string rawQuery = await reader.ReadToEndAsync();

                    InstanceType instanceType = this.GetInstanceType();
                    string connectionString = this.GetConnectionString(database);
                    connectionString = ConnectionStringBuilder.SetupTimeout(instanceType, connectionString, timeLimit);
                    connectionString = ConnectionStringBuilder.SetupCommandTimeout(instanceType, connectionString, timeLimit);

                    await using (ConnectorBase connector = this.ConnectorFactory
                                     .Create(this.GetInstanceType(), connectionString))
                    {
                        Query data = new Query();

                        await connector.OpenConnectionAsync();

                        try
                        {
                            await using (DbCommand query = connector.CreateCommand(rawQuery))
                            {
                                DateTime start = DateTime.UtcNow;
                                data.RecordsAffected = await query.ExecuteNonQueryAsync();
                                DateTime end = DateTime.UtcNow;
                                data.ElapsedTimeMs = (end - start).ToElapsedTimeMs();
                            }
                        }
                        catch (Exception exception)
                        {
                            if (exception.InnerException?.GetType() == typeof(TimeoutException))
                            {
                                response.ErrorMessage = $"The query has exceeded the time limit of {timeLimit}s. Try to chunk your query.";
                            }
                            else
                            {
                                throw;
                            }
                        }

                        if (response.ErrorMessage.IsNullOrEmpty())
                            response.Data = data;
                    }
                }
            }
        }

        return response;
    }

    /// <summary>
    /// Imports data from a DSV file into a database table.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    /// <param name="tableName">The name of the table where data will be imported.</param>
    /// <param name="request">The request containing the DSV file and import options.</param>
    /// <returns>A response object containing information about the import operation.</returns>
    [HttpPost("import/{database}/dsv/{tableName}"), DisableRequestSizeLimit]
    public async Task<Response<Query>> ImportDsvAsync(
        [FromRoute] string database,
        [FromRoute] string tableName,
        [FromForm] QueryImportDsvRequest request)
    {
        Response<Query> response = new();

        const short timeLimit = 180;

        string fileExtension = request.File.FileName.Split('.').Last();

        string[] allowedExtensions = { "csv", "dsv", "tsv" };

        if (!allowedExtensions.Contains(fileExtension))
        {
            this.Response.StatusCode = 400;
        }
        else if (request.File.Length == 0)
        {
            this.Response.StatusCode = 400;
            response.ErrorMessage = $"'{request.File.FileName}' is empty.";
        }
        else
        {
            InstanceType instanceType = this.GetInstanceType();
            string connectionString = this.GetConnectionString(database);
            connectionString = ConnectionStringBuilder.SetupTimeout(instanceType, connectionString, timeLimit);
            connectionString = ConnectionStringBuilder.SetupCommandTimeout(instanceType, connectionString, timeLimit);

            await using (ConnectorBase connector = this.ConnectorFactory
                             .Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                DateTime start = DateTime.UtcNow;

                ImportResult importResult = await connector
                    .ExecuteDsvImportAsync(
                        file: request.File,
                        table: tableName,
                        columnSeparator: request.Separator,
                        chunkSize: request.ChunkSize,
                        doubleQuotes: request.DoubleQuotes,
                        rollbackOnError: request.RollbackOnError,
                        rowsToSkip: request.RowsToSkip);

                DateTime end = DateTime.UtcNow;

                if (importResult.RecordsAffected.HasValue)
                {
                    response.Data = new()
                    {
                        RecordsAffected = importResult.RecordsAffected,
                        ElapsedTimeMs = (end - start).ToElapsedTimeMs()
                    };
                }

                if (!importResult.ErrorMessage.IsNullOrEmpty())
                {
                    response.ErrorMessage = importResult.ErrorMessage;
                }
            }
        }

        return response;
    }
}