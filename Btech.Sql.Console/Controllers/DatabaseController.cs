using System.Data;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models;
using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Models.Responses.Database;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Controllers;

[Controller]
[Route("/api/databases")]
public class DatabaseController : SessionRelatedControllerBase
{
    public DatabaseController(ILogger<DatabaseController> logger, IConnectorFactory connectorFactory, ISessionStorage<SessionData> sessionStorage)
        : base(logger, sessionStorage)
    {
        this.ConnectorFactory = connectorFactory;
    }

    private IConnectorFactory ConnectorFactory { get; }

    [HttpGet]
    public async Task<PaginationResponse<Database>> GetDatabases(
        [FromQuery] int page = 0, [FromQuery] int perPage = 5, [FromQuery] string search = null)
    {
        if (page < 0)
            page = 0;

        if (perPage < 1)
            perPage = 1;

        List<string> databaseList = new();

        await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), this.GetConnectionString()))
        {
            await connector.OpenConnectionAsync();

            DataTable schema = await connector.Connection.GetSchemaAsync("Databases");

            foreach (DataRow dataRow in schema.AsEnumerable())
            {
                string databaseName = dataRow["database_name"].ToString();

                if (databaseName?.StartsWith("template") == false)
                {
                    if (!search.IsNullOrEmpty())
                    {
                        if (databaseName.Contains(search!, StringComparison.OrdinalIgnoreCase))
                            databaseList.Add(databaseName);
                    }
                    else
                        databaseList.Add(databaseName);
                }
            }
        }

        return new()
        {
            PerPage = perPage,
            CurrentPage = page,
            TotalAmount = databaseList.Count,
            Entities = databaseList
                .OrderBy(databaseName => databaseName)
                .Skip(page * perPage)
                .Take(perPage)
                .Select(
                    databaseName => new Database
                    {
                        Name = databaseName
                    })
                .ToList()
        };
    }

    [HttpGet("{name}")]
    public async Task<Response<Database>> GetDatabaseSchema([FromRoute] string name)
    {
        Response<Database> response = new Response<Database>
        {
            Data = new Database
            {
                Name = name,
                Tables = new List<Table>()
            }
        };

        string connectionString = this.GetConnectionString(name);

        await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
        {
            await connector.OpenConnectionAsync();

            DataTable tables = await connector.Connection.GetSchemaAsync("Tables");

            foreach (DataRow dataRow in tables.Rows)
            {
                string tableName = dataRow.Field<string>("table_name");

                if (!tableName.IsNullOrEmpty())
                {
                    Table table = new Table
                    {
                        Name = tableName,
                        Columns = new List<Column>()
                    };

                    DataTable columns = await connector.Connection
                        .GetSchemaAsync("Columns", new[] { null, null, tableName });

                    DataTable indexColumns = await connector.Connection
                        .GetSchemaAsync("IndexColumns", new[] { null, null, tableName });

                    int indexesFound = 0;

                    foreach (DataRow column in columns.Rows)
                    {
                        string columnName = column["column_name"].ToString();
                        string type = column["data_type"].ToString();
                        int? maxLength = null;
                        bool? isPrimaryKey = null;
                        bool? isForeignKey = null;

                        if (indexColumns.Rows.Count > 0 && indexesFound < indexColumns.Rows.Count)
                        {
                            foreach (DataRow indexColumn in indexColumns.Rows)
                            {
                                if (indexColumn["column_name"].ToString() == columnName &&
                                    (indexColumn["index_name"].ToString()?.Contains("pk", StringComparison.OrdinalIgnoreCase) == true ||
                                     indexColumn["constraint_name"].ToString()?.Contains("pk", StringComparison.OrdinalIgnoreCase) == true))
                                {
                                    isPrimaryKey = true;
                                    indexesFound++;
                                }
                                else if (indexColumn["column_name"].ToString() == columnName &&
                                         (indexColumn["index_name"].ToString()?.Contains("fk", StringComparison.OrdinalIgnoreCase) == true ||
                                          indexColumn["constraint_name"].ToString()?.Contains("fk", StringComparison.OrdinalIgnoreCase) == true))
                                {
                                    isForeignKey = true;
                                    indexesFound++;
                                }
                            }
                        }

                        string characterMaximumLength;

                        if (!(characterMaximumLength = column["character_maximum_length"].ToString()).IsNullOrEmpty())
                        {
                            maxLength = characterMaximumLength.ToNullableInt();
                        }

                        table.Columns.Add(
                            new Column
                            {
                                Name = columnName,
                                Type = type,
                                MaxLength = maxLength,
                                IsPrimaryKey = isPrimaryKey,
                                IsForeignKey = isForeignKey
                            });
                    }

                    response.Data.Tables.Add(table);
                }
            }
        }

        return response;
    }
}