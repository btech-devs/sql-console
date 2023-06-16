using System.Data;
using System.Data.Common;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Models.Responses.Connector;

namespace Btech.Sql.Console.Base;

/// <summary>
/// The <c>ConnectorBase</c> class is a base class that represents connection
/// features to database instances, such as PostgreSQL and SQL Server.
/// </summary>
public abstract class ConnectorBase : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <c>ConnectorBase</c> class with <paramref name="connectionString"/>.
    /// </summary>
    /// <param name="connectionString">The ConnectionString is used to establish a connection to a database instance.</param>
    protected ConnectorBase(string connectionString)
    {
        this.ConnectionString = connectionString;
    }

    #region Public Properties

    /// <summary>
    /// Represents a connection to a database.
    /// </summary>
    public abstract DbConnection Connection { get; }

    /// <summary>
    /// The ConnectionString is used to establish a connection to a database instance.
    /// </summary>
    public string ConnectionString { get; }

    #endregion Public Properties

    #region Private Methods

    private void Dispose(bool dispose)
    {
        if (!this._disposed && dispose)
        {
            this.Connection.Dispose();
            this._disposed = true;
        }
    }

    #endregion Private Methods

    #region Abstract Methods

    /// <summary>
    /// Initializes a new instance of the DbCommand class with the text of the query.
    /// </summary>
    /// <param name="sql">The SQL query.</param>
    /// <returns></returns>
    public abstract DbCommand CreateCommand(string sql = null);

    /// <summary>
    /// Verifies whether the <paramref name="columnDataType"/> requires double quotes in the generated SQL query.
    /// </summary>
    /// <param name="columnDataType">The column data type.</param>
    protected abstract bool NeedQuotes(string columnDataType);

    /// <summary>
    /// Method generate a SQL query from <paramref name="rows"/> with <paramref name="table"/>.
    /// </summary>
    /// <param name="header">The list contains the names of columns, along with a boolean flag indicating whether each column's name should be surrounded by quotes in the SQL query.</param>
    /// <param name="table">The target table to insert into.</param>
    /// <param name="rows">The rows contain the values of the cells to be inserted.</param>
    /// <returns></returns>
    protected abstract string ConvertToInsertSql(
        List<(string ColumnName, bool isQuoted)> header, string table, params List<string>[] rows);

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(dispose: true);
        GC.SuppressFinalize(this);
    }

    #endregion Abstract Methods

    #region Public Methods

    /// <summary>
    /// Executes a DSV (delimiter separated values) import from a file into a database table asynchronously.
    /// </summary>
    /// <param name="file">The DSV file to import.</param>
    /// <param name="table">The target database table to import the data into.</param>
    /// <param name="columnSeparator">The character used to separate columns in the DSV file. Default is comma (,).</param>
    /// <param name="chunkSize">The number of rows to import in a single database transaction. Default is 10.</param>
    /// <param name="doubleQuotes">Indicates if double quotes should be used to enclose string values in the DSV file. Default is false.</param>
    /// <param name="rollbackOnError">Indicates if the transaction should be rolled back if an error occurs during import. Default is false.</param>
    /// <param name="rowsToSkip">The number of rows to skip at the beginning of the DSV file. Default is 0.</param>
    /// <returns>An <see cref="ImportResult"/> object containing information about the import process.</returns>
    public async Task<ImportResult> ExecuteDsvImportAsync(
        IFormFile file,
        string table,
        char columnSeparator = ',',
        uint chunkSize = 10,
        bool doubleQuotes = false,
        bool rollbackOnError = false,
        uint rowsToSkip = 0)
    {
        ImportResult importResult = new();

        DbTransaction dbTransaction = null;

        try
        {
            List<DataRow> columns = (await this.Connection
                    .GetSchemaAsync("Columns", new[] { null, null, table }))
                .AsEnumerable()
                .ToList();

            if (rollbackOnError)
                dbTransaction = await this.Connection.BeginTransactionAsync();

            await using (Stream stream = file.OpenReadStream())
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    List<(string ColumnName, bool isQuote)> header = null;

                    List<List<string>> chunk = new();

                    int lineNo = 0;

                    string separator = doubleQuotes ? $"\"{columnSeparator}\"" : columnSeparator.ToString();

                    while (!streamReader.EndOfStream && importResult.ErrorMessage.IsNullOrEmpty())
                    {
                        List<string> row = (await streamReader.ReadLineAsync())?.Split(separator).ToList();

                        if (row?.Any() is true)
                        {
                            if (doubleQuotes)
                            {
                                row[0] = row[0].Remove(0, 1);
                                row[^1] = row[^1].Remove(row[^1].Length - 1);
                            }

                            if (header != null)
                            {
                                lineNo++;

                                if (lineNo <= rowsToSkip)
                                    continue;
                            }

                            if (header == null)
                            {
                                foreach (string columnName in row)
                                {
                                    DataRow columnInfo = columns
                                        .FirstOrDefault(column => column["column_name"].ToString() == columnName);

                                    if (columnInfo == default)
                                    {
                                        importResult.ErrorMessage = $"Table '{table}' does not contain column with name '{columnName}'.";

                                        break;
                                    }

                                    header ??= new();
                                    header.Add((columnName, this.NeedQuotes(columnInfo["data_type"].ToString())));
                                }
                            }
                            else if (row.Count != header.Count)
                            {
                                importResult.ErrorMessage = $"Row column count is unexpected: '{row.Count}'. Expected: '{header.Count}'. LineNo: '{lineNo}'.";
                            }
                            else
                            {
                                chunk.Add(row);

                                if (chunk.Count == chunkSize)
                                {
                                    string sql = this.ConvertToInsertSql(header, table, chunk.ToArray());

                                    await using (DbCommand dbCommand = this.CreateCommand(sql))
                                    {
                                        if (dbTransaction != null)
                                            dbCommand.Transaction = dbTransaction;

                                        importResult.RecordsAffected ??= 0;
                                        importResult.RecordsAffected += await dbCommand.ExecuteNonQueryAsync();
                                    }

                                    chunk.Clear();
                                }
                            }
                        }
                    }

                    if (chunk.Any() && importResult.ErrorMessage.IsNullOrEmpty())
                    {
                        string sql = this.ConvertToInsertSql(header, table, chunk.ToArray());

                        await using (DbCommand dbCommand = this.CreateCommand(sql))
                        {
                            if (dbTransaction != null)
                                dbCommand.Transaction = dbTransaction;

                            importResult.RecordsAffected ??= 0;
                            importResult.RecordsAffected += await dbCommand.ExecuteNonQueryAsync();
                        }

                        chunk.Clear();
                    }
                    else if (lineNo <= rowsToSkip)
                    {
                        importResult.RecordsAffected ??= 0;
                    }
                }
            }

            if (dbTransaction != null)
            {
                if (importResult.ErrorMessage.IsNullOrEmpty())
                    await dbTransaction.CommitAsync();
                else
                    importResult.RecordsAffected = 0;
            }
        }
        catch (Exception exception)
        {
            importResult.ErrorMessage = exception.Message;
        }
        finally
        {
            if (dbTransaction != null)
            {
                await dbTransaction.DisposeAsync();
            }
        }

        return importResult;
    }

    /// <summary>
    /// Asynchronously opens a database connection using the <see cref="Connection"/> property.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OpenConnectionAsync() => await this.Connection.OpenAsync();

    /// <summary>
    /// Closes the connection to the database asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CloseConnectionAsync() => await this.Connection.CloseAsync();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!this._disposed)
        {
            await this.CloseConnectionAsync();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    #endregion Public Methods
}