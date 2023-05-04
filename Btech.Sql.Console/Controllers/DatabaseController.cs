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
        InstanceType instanceType = this.GetInstanceType();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            response.Data = new Database
            {
                Name = databaseName,
                Schemas = new List<Schema>()
            };

            string connectionString = this.GetConnectionString(databaseName);

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                DataTable schemas = new();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = "SELECT schema_name FROM information_schema.schemata;";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = "SELECT [SCHEMA_NAME] FROM [INFORMATION_SCHEMA].[SCHEMATA];";

                            break;
                    }

                    await using (DbDataReader reader = await dbCommand.ExecuteReaderAsync())
                    {
                        schemas.Load(reader);
                    }
                }

                foreach (DataRow dataRow in schemas.Rows)
                {
                    string schemaName = dataRow[0].ToString();

                    if (!schemaName.IsNullOrEmpty())
                    {
                        response.Data.Schemas.Add(new Schema
                        {
                            Name = schemaName
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

    [HttpGet("{databaseName}/{schemaName}/views")]
    public async Task<Response<Schema>> GetSchemaViewsAsync([FromRoute] string databaseName, [FromRoute] string schemaName)
    {
        Response<Schema> response = new Response<Schema>();
        InstanceType instanceType = this.GetInstanceType();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            response.Data = new Schema
            {
                Name = databaseName,
                Views = new List<View>()
            };

            string connectionString = this.GetConnectionString(databaseName);

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                DataTable views = new();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = $"SELECT table_name FROM information_schema.views where table_schema = '{schemaName}';";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = $"SELECT [TABLE_NAME] FROM [INFORMATION_SCHEMA].[VIEWS] where [TABLE_SCHEMA] = '{schemaName}';";

                            break;
                    }

                    await using (DbDataReader reader = await dbCommand.ExecuteReaderAsync())
                    {
                        views.Load(reader);
                    }
                }

                foreach (DataRow dataRow in views.Rows)
                {
                    string tableName = dataRow[0].ToString();

                    if (!tableName.IsNullOrEmpty())
                    {
                        response.Data.Views.Add(
                            new View
                            {
                                Name = tableName
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

    [HttpGet("{databaseName}/{schemaName}/views/{viewName}")]
    public async Task<Response<View>> GetViewSchema(
        [FromRoute] string databaseName, [FromRoute] string schemaName, [FromRoute] string viewName)
    {
        Response<View> response = new Response<View>
        {
            Data = new View
            {
                Columns = new()
            }
        };

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            string connectionString = this.GetConnectionString(databaseName);
            InstanceType instanceType = this.GetInstanceType();

            DataTable viewSchema = new();

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = $"SELECT column_name, data_type, character_maximum_length FROM information_schema.columns where table_schema = '{schemaName}' and table_name = '{viewName}' ORDER BY ordinal_position;";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = $"SELECT [COLUMN_NAME], [DATA_TYPE], [CHARACTER_MAXIMUM_LENGTH] FROM [INFORMATION_SCHEMA].[COLUMNS] where [TABLE_SCHEMA] = '{schemaName}' and [TABLE_NAME] = '{viewName}' ORDER BY [ORDINAL_POSITION];";

                            break;
                    }

                    await using (DbDataReader reader = await dbCommand.ExecuteReaderAsync())
                    {
                        viewSchema.Load(reader);
                    }
                }
            }

            foreach (DataRow row in viewSchema.Rows)
            {
                response.Data.Columns.Add(new()
                {
                    Name = row[0].ToString(),
                    Type = row[1].ToString(),
                    MaxLength = row[2].ToNullableInt()
                });
            }
        }
        else
        {
            response.ErrorMessage = "You do not have permission for this request.";
        }

        return response;
    }

    [HttpGet("{databaseName}/{schemaName}/routines")]
    public async Task<Response<Schema>> GetSchemaRoutinesAsync([FromRoute] string databaseName, [FromRoute] string schemaName)
    {
        Response<Schema> response = new Response<Schema>();
        InstanceType instanceType = this.GetInstanceType();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            response.Data = new Schema
            {
                Name = schemaName,
                Routines = new List<Routine>()
            };

            string connectionString = this.GetConnectionString(databaseName);

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                DataTable schemas = new();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = $"SELECT DISTINCT routine_name, routine_type FROM information_schema.routines where specific_schema = '{schemaName}' ORDER BY routine_name;";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = $"SELECT DISTINCT [ROUTINE_NAME], [ROUTINE_TYPE] FROM [INFORMATION_SCHEMA].[ROUTINES] where [SPECIFIC_SCHEMA] = '{schemaName}' ORDER BY [ROUTINE_NAME];";

                            break;
                    }

                    await using (DbDataReader reader = await dbCommand.ExecuteReaderAsync())
                    {
                        schemas.Load(reader);
                    }
                }

                foreach (DataRow dataRow in schemas.Rows)
                {
                    string routineName = dataRow[0].ToString();
                    string routineType = dataRow[1].ToString();

                    if (!routineName.IsNullOrEmpty())
                    {
                        response.Data.Routines.Add(
                            new Routine
                            {
                                Name = routineName,
                                Type = routineType
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

    [HttpGet("{databaseName}/{schemaName}/tables/{tableName}")]
    public async Task<Response<Table>> GetTableSchema(
        [FromRoute] string databaseName, [FromRoute] string schemaName, [FromRoute] string tableName)
    {
        Response<Table> response = new Response<Table>();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            string connectionString = this.GetConnectionString(databaseName);
            InstanceType instanceType = this.GetInstanceType();

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                response.Data = new Table
                {
                    Name = tableName,
                    Columns = new(),
                    Constraints = new(),
                    Indexes = new()
                };

                DataTable columns = await connector.Connection
                    .GetSchemaAsync("Columns", new[] { null, null, tableName });

                // Constraints
                DataTable constraints = new();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = "SELECT tableConstraints.constraint_name, " +
                                                    "tableConstraints.constraint_type, " +
                                                    "keyColumnUsage.table_name, " +
                                                    "keyColumnUsage.column_name, " +
                                                    "constraintColumnUsage.table_name, " +
                                                    "constraintColumnUsage.column_name " +
                                                    "FROM information_schema.table_constraints as tableConstraints " +
                                                    "LEFT JOIN information_schema.key_column_usage as keyColumnUsage " +
                                                    "ON tableConstraints.constraint_name = keyColumnUsage.constraint_name " +
                                                    "LEFT JOIN  information_schema.constraint_column_usage as constraintColumnUsage " +
                                                    "ON constraint_type = 'FOREIGN KEY' and tableConstraints.constraint_name = constraintColumnUsage.constraint_name " +
                                                    $"where tableConstraints.table_schema = '{schemaName}' and tableConstraints.table_name = '{tableName}'";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = "SELECT tableConstraints.CONSTRAINT_NAME, " +
                                                    "tableConstraints.CONSTRAINT_TYPE, " +
                                                    "keyColumnUsage.TABLE_NAME, " +
                                                    "keyColumnUsage.COLUMN_NAME, " +
                                                    "constraintColumnUsage.TABLE_NAME, " +
                                                    "constraintColumnUsage.COLUMN_NAME " +
                                                    "FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS as tableConstraints " +
                                                    "LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE as keyColumnUsage " +
                                                    "ON tableConstraints.CONSTRAINT_NAME = keyColumnUsage.CONSTRAINT_NAME " +
                                                    "LEFT JOIN  INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE as constraintColumnUsage " +
                                                    "ON CONSTRAINT_TYPE = 'FOREIGN KEY' and tableConstraints.CONSTRAINT_NAME = constraintColumnUsage.CONSTRAINT_NAME " +
                                                    $"where tableConstraints.table_schema = '{schemaName}' and tableConstraints.table_name = '{tableName}'";

                            break;
                    }

                    await using (DbDataReader dbDataReader = await dbCommand.ExecuteReaderAsync())
                    {
                        constraints.Load(dbDataReader);
                    }

                    if (instanceType == InstanceType.MsSql)
                    {
                        DataTable spFkeys = new DataTable();

                        dbCommand.CommandText = $"EXEC sp_fkeys  @fktable_owner = '{schemaName}', @fktable_name = '{tableName}';";

                        await using (DbDataReader dbDataReader = await dbCommand.ExecuteReaderAsync())
                        {
                            spFkeys.Load(dbDataReader);
                        }

                        foreach (DataRow constraint in constraints.Rows)
                        {
                            if (constraint[1].ToString() == "FOREIGN KEY")
                            {
                                DataRow? spFkeyRow = null;

                                foreach (DataRow foreignKeyRow in spFkeys.Rows)
                                {
                                    if (foreignKeyRow["FK_NAME"].ToString() == constraint[0].ToString())
                                    {
                                        spFkeyRow = foreignKeyRow;

                                        break;
                                    }
                                }

                                if (spFkeyRow != null)
                                {
                                    constraint[4] = spFkeyRow[2];
                                    constraint[5] = spFkeyRow[3];
                                }
                            }
                        }
                    }
                }

                if (columns.Rows.Count > 0)
                {
                    DataTable sequences = new DataTable();

                    switch (instanceType)
                    {
                        case InstanceType.PgSql:

                            await using (DbCommand dbCommand = connector
                                             .CreateCommand("SELECT sequence_name,start_value,increment FROM information_schema.sequences"))
                            {
                                await using (DbDataReader dataReader = await dbCommand.ExecuteReaderAsync())
                                    sequences.Load(dataReader);
                            }

                            break;
                        case InstanceType.MsSql:
                        {
                            await using (DbCommand dbCommand = connector
                                             .CreateCommand($"SELECT name, increment_value, seed_value FROM sys.identity_columns where object_id = object_id('{tableName}')"))
                            {
                                await using (DbDataReader dataReader = await dbCommand.ExecuteReaderAsync())
                                    sequences.Load(dataReader);
                            }

                            break;
                        }
                    }

                    foreach (DataRow column in columns.Rows)
                    {
                        string columnName = column["column_name"].ToString();
                        string type = column["data_type"].ToString();
                        int? maxLength = null;
                        bool isPrimaryKey = false;
                        bool isForeignKey = false;

                        foreach (DataRow constraint in constraints.Rows)
                        {
                            if (constraint[1].ToString() == "PRIMARY KEY" && constraint[3].ToString() == columnName)
                            {
                                isPrimaryKey = true;

                                break;
                            }
                        }

                        foreach (DataRow constraint in constraints.Rows)
                        {
                            if (constraint[1].ToString() == "FOREIGN KEY" && constraint[3].ToString() == columnName)
                            {
                                isForeignKey = true;

                                break;
                            }
                        }

                        string characterMaximumLength;

                        if (!(characterMaximumLength = column["character_maximum_length"].ToString()).IsNullOrEmpty())
                        {
                            maxLength = characterMaximumLength.ToNullableInt();
                        }

                        string defaultValue = column["column_default"].ToString();

                        // Map auto increment values
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

                // Map Constraints
                foreach (DataRow constraint in constraints.Rows)
                {
                    string name = constraint[0].ToString();
                    string type = constraint[1].ToString();
                    string sourceTable = constraint[2].ToString();
                    string sourceColumn = constraint[3].ToString();
                    string targetTable = constraint[4].ToString();
                    string targetColumn = constraint[5].ToString();

                    response.Data.Constraints.Add(
                        new()
                        {
                            Name = name,
                            Type = type,
                            SourceTable = sourceTable,
                            SourceColumn = sourceColumn,
                            TargetTable = targetTable,
                            TargetColumn = targetColumn
                        });
                }

                // Map Indexes
                DataTable indexes = new DataTable();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = $"SELECT indexname FROM pg_catalog.pg_indexes where schemaname = '{schemaName}' and tablename = '{tableName}';";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = $"select name FROM sys.indexes where object_id = OBJECT_ID('{schemaName}.{tableName}');";

                            break;
                    }

                    await using (DbDataReader dbDataReader = await dbCommand.ExecuteReaderAsync())
                    {
                        indexes.Load(dbDataReader);
                    }
                }

                foreach (DataRow index in indexes.Rows)
                {
                    response.Data.Indexes.Add(
                        new()
                        {
                            Name = index[0].ToString()
                        });
                }
            }
        }
        else
        {
            response.ErrorMessage = "You do not have permission for this request.";
        }

        return response;
    }

    [HttpGet("{databaseName}/{schemaName}/tables")]
    public async Task<Response<Schema>> GetSchemaTablesAsync([FromRoute] string databaseName, [FromRoute] string schemaName)
    {
        Response<Schema> response = new Response<Schema>();
        InstanceType instanceType = this.GetInstanceType();

        if (PermissionRuleFactory.IsAllowedDatabase(databaseName, this.GetUserRole(), this.GetDatabaseHost()))
        {
            response.Data = new Schema
            {
                Name = schemaName,
                Tables = new List<Table>()
            };

            string connectionString = this.GetConnectionString(databaseName);

            await using (ConnectorBase connector = this.ConnectorFactory.Create(this.GetInstanceType(), connectionString))
            {
                await connector.OpenConnectionAsync();

                DataTable tables = new();
                DataTable columns = new();

                await using (DbCommand dbCommand = connector.CreateCommand())
                {
                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = $"SELECT table_name FROM information_schema.tables where table_schema = '{schemaName}';";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = $"SELECT [TABLE_NAME] FROM [INFORMATION_SCHEMA].[TABLES] where [TABLE_SCHEMA] = '{schemaName}';";

                            break;
                    }

                    await using (DbDataReader reader = await dbCommand.ExecuteReaderAsync())
                    {
                        tables.Load(reader);
                    }

                    switch (instanceType)
                    {
                        case InstanceType.PgSql:
                            dbCommand.CommandText = $"SELECT table_name, column_name FROM information_schema.columns WHERE table_schema='{schemaName}' ORDER BY ordinal_position;";

                            break;
                        case InstanceType.MsSql:
                            dbCommand.CommandText = $"SELECT [TABLE_NAME], [COLUMN_NAME] FROM [INFORMATION_SCHEMA].[COLUMNS] WHERE [TABLE_SCHEMA]='{schemaName}' ORDER BY [ORDINAL_POSITION]";

                            break;
                    }

                    await using (DbDataReader reader = await dbCommand.ExecuteReaderAsync())
                    {
                        columns.Load(reader);
                    }
                }

                foreach (DataRow dataRow in tables.Rows)
                {
                    string tableName = dataRow[0].ToString();
                    List<Column> tableColumns = new List<Column>();

                    foreach (DataRow columnsRow in columns.Rows)
                    {
                        if (columnsRow[0].ToString() == tableName)
                            tableColumns.Add(new Column
                            {
                                Name = columnsRow[1].ToString()
                            });
                    }

                    if (!tableName.IsNullOrEmpty())
                    {
                        response.Data.Tables.Add(
                            new Table
                            {
                                Name = tableName,
                                Columns = tableColumns
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