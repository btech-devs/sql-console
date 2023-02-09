using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using System.Data.Common;
using Btech.Sql.Console.Attributes;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Requests.Query;
using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Models.Responses.Query;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Controllers;

[Controller]
[Route("api/query")]
public class QueryController : SessionRelatedControllerBase
{
    public QueryController(
        ILogger<QueryController> logger, IConnectorFactory connectorFactory, ISessionStorage<SessionData> sessionStorage)
        : base(logger, sessionStorage)
    {
        this.ConnectorFactory = connectorFactory;
    }

    private IConnectorFactory ConnectorFactory { get; }

    [HttpPost("execute")]
    [ValidateModel]
    public async Task<Response<Query>> ExecuteAsync([FromBody] QueryExecuteRequest queryExecuteRequest)
    {
        Response<Query> queryResponse = new();

        await using (ConnectorBase connector = this.ConnectorFactory
                         .Create(this.GetInstanceType(), this.GetConnectionString(queryExecuteRequest.DatabaseName)))
        {
            Query data = new Query();

            await connector.OpenConnectionAsync();

            DataTable dataTable = new DataTable();
            ReadOnlyCollection<DbColumn> columnsSchema;

            await using (DbCommand query = connector.CreateCommand(queryExecuteRequest.Sql))
            {
                await using (DbDataReader reader = await query.ExecuteReaderAsync())
                {
                    columnsSchema = await reader.GetColumnSchemaAsync();
                    DateTime start = DateTime.UtcNow;
                    dataTable.Load(reader);
                    DateTime end = DateTime.UtcNow;
                    data.RecordsAffected = reader.RecordsAffected;
                    data.ElapsedTimeMs = Math.Truncate((decimal) Math.Round((end - start).TotalMilliseconds, 3) * 1000m) / 1000m;
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

        return queryResponse;
    }

    [HttpPost("execute/dsv")]
    [ValidateModel]
    public async Task ExecuteDsvAsync([FromBody] QueryExecuteDsvRequest queryExecuteDsvRequest)
    {
        // TODO: catch expected exceptions

        this.HttpContext.Features.Get<IHttpResponseBodyFeature>()!.DisableBuffering();

        await using (ConnectorBase connector = this.ConnectorFactory
                         .Create(this.GetInstanceType(), this.GetConnectionString(queryExecuteDsvRequest.DatabaseName)))
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

                                columnList.Add(value);
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

        this.LogDebug("Close connection.");
    }
}