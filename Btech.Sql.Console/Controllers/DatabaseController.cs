using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Factories;
using Btech.Sql.Console.Interfaces;
using Btech.Sql.Console.Models.Responses.Base;
using Btech.Sql.Console.Models.Responses.Database;
using Microsoft.AspNetCore.Mvc;

namespace Btech.Sql.Console.Controllers;

[Controller]
[Route("/api/databases")]
public class DatabaseController : SessionRelatedControllerBase
{
    public DatabaseController(
        ILogger<DatabaseController> logger, IConnectorFactory connectorFactory)
        : base(logger)
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

            foreach (DataRow dataRow in schema.AsEnumerable().ToList())
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

        databaseList = PermissionRuleFactory.FilterDatabaseNames(databaseList, this.GetUserRole(), this.GetDatabaseHost());

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

    [HttpGet("{databaseName}")]
    public async Task<Response<Database>> GetDatabaseSchema([FromRoute] string databaseName)
    {
        Response<Database> response = new Response<Database>();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            response.Data = new Database
            {
                Name = databaseName,
                Tables = new List<Table>()
            };

            string connectionString = this.GetConnectionString(databaseName);

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

                        foreach (DataRow column in columns.Rows)
                        {
                            table.Columns.Add(new Column
                            {
                                Name = column["column_name"].ToString()
                            });
                        }

                        response.Data.Tables.Add(table);
                    }
                }
            }
        }
        else
        {
            response.ErrorMessage = "You do not have permission for this request.";
        }

        return response;
    }

    [HttpGet("{databaseName}/{tableName}")]
    public async Task<Response<Table>> GetTableSchema([FromRoute] string databaseName, [FromRoute] string tableName)
    {
        Response<Table> response = new Response<Table>();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            string connectionString = this.GetConnectionString(databaseName);

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                response.Data = new Table
                {
                    Name = tableName,
                    Columns = new List<Column>()
                };

                DataTable columns = await connector.Connection
                    .GetSchemaAsync("Columns", new[] { null, null, tableName });

                if (columns.Rows.Count > 0)
                {
                    DataTable constraintColumns = null;
                    DataTable sequences = new DataTable();

                    InstanceType instanceType = this.GetInstanceType();

                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            constraintColumns = await connector.Connection
                                .GetSchemaAsync(
                                    collectionName: "ConstraintColumns",
                                    restrictionValues: new[] { null, null, tableName });

                            await using (DbCommand dbCommand = connector
                                             .CreateCommand("SELECT sequence_name,start_value,increment FROM information_schema.sequences"))
                            {
                                await using (DbDataReader dataReader = await dbCommand.ExecuteReaderAsync())
                                    sequences.Load(dataReader);
                            }

                            break;
                        case InstanceType.MsSql:
                        {
                            constraintColumns = new DataTable();

                            await using (DbCommand dbCommand = connector
                                             .CreateCommand($"SELECT * FROM INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE where TABLE_NAME = '{tableName}' and CONSTRAINT_CATALOG = '{databaseName}'"))
                            {
                                await using (DbDataReader dataReader = await dbCommand.ExecuteReaderAsync())
                                    constraintColumns.Load(dataReader);
                            }

                            await using (DbCommand dbCommand = connector
                                             .CreateCommand($"SELECT name, increment_value, seed_value FROM sys.identity_columns where object_id = object_id('{tableName}')"))
                            {
                                await using (DbDataReader dataReader = await dbCommand.ExecuteReaderAsync())
                                    sequences.Load(dataReader);
                            }

                            break;
                        }
                    }

                    int indexesFound = 0;

                    foreach (DataRow column in columns.Rows)
                    {
                        string columnName = column["column_name"].ToString();
                        string type = column["data_type"].ToString();
                        int? maxLength = null;
                        bool? isPrimaryKey = null;
                        bool? isForeignKey = null;

                        if (constraintColumns?.Rows.Count > 0 && indexesFound < constraintColumns.Rows.Count)
                        {
                            foreach (DataRow constraintColumn in constraintColumns.Rows)
                            {
                                string constraintColumnName = constraintColumn["column_name"].ToString();

                                switch (instanceType)
                                {
                                    case InstanceType.PgSql:
                                        string constraintType = constraintColumn["constraint_type"].ToString();

                                        if (constraintColumnName == columnName &&
                                            constraintType == "PRIMARY KEY")
                                        {
                                            isPrimaryKey = true;
                                            indexesFound++;
                                        }
                                        else if (constraintColumnName == columnName &&
                                                 constraintType == "FOREIGN KEY")
                                        {
                                            isForeignKey = true;
                                            indexesFound++;
                                        }

                                        break;
                                    case InstanceType.MsSql:
                                        string constraintName = constraintColumn["constraint_name"].ToString();

                                        if (constraintColumnName == columnName &&
                                            constraintName
                                                ?.Contains("pk", StringComparison.OrdinalIgnoreCase) ==
                                            true)
                                        {
                                            isPrimaryKey = true;
                                            indexesFound++;
                                        }
                                        else if (constraintColumnName == columnName &&
                                                 constraintName
                                                     ?.Contains("fk", StringComparison.OrdinalIgnoreCase) ==
                                                 true)
                                        {
                                            isForeignKey = true;
                                            indexesFound++;
                                        }

                                        break;
                                }
                            }
                        }

                        string characterMaximumLength;

                        if (!(characterMaximumLength = column["character_maximum_length"].ToString()).IsNullOrEmpty())
                        {
                            maxLength = characterMaximumLength.ToNullableInt();
                        }

                        string defaultValue = column["column_default"].ToString();

                        switch (instanceType)
                        {
                            case InstanceType.PgSql:
                                if (defaultValue?.StartsWith("nextval") == true)
                                {
                                    foreach (DataRow sequence in sequences.Rows)
                                    {
                                        string sequenceName = sequence["sequence_name"].ToString();
                                        string sequenceStartValue = sequence["start_value"].ToString();
                                        string sequenceIncrement = sequence["increment"].ToString();

                                        if (!sequenceStartValue.IsNullOrEmpty() &&
                                            !sequenceIncrement.IsNullOrEmpty() &&
                                            defaultValue.Contains(sequenceName))
                                        {
                                            defaultValue = $"nextval({sequenceStartValue},{sequenceIncrement})";

                                            break;
                                        }
                                    }
                                }
                                else if (!defaultValue.IsNullOrEmpty())
                                    defaultValue = Regex.Replace(defaultValue!, @"::([\w\s])+", string.Empty);

                                break;
                            case InstanceType.MsSql:
                                DataRow identity = sequences
                                    .AsEnumerable()
                                    .FirstOrDefault(row => row["name"].ToString() == columnName);

                                if (identity != default)
                                {
                                    string incrementValue = identity["increment_value"].ToString();
                                    string seedValue = identity["seed_value"].ToString();

                                    if (!incrementValue.IsNullOrEmpty() && !seedValue.IsNullOrEmpty())
                                        defaultValue = $"autoincrement({seedValue},{incrementValue})";
                                }

                                break;
                        }

                        response.Data.Columns.Add(
                            new Column
                            {
                                Name = columnName,
                                DefaultValue = defaultValue,
                                Type = type,
                                MaxLength = maxLength,
                                IsPrimaryKey = isPrimaryKey,
                                IsForeignKey = isForeignKey
                            });
                    }
                }
            }
        }
        else
        {
            response.ErrorMessage = "You do not have permission for this request.";
        }

        return response;
    }
}