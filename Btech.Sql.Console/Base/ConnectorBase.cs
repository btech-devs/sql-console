using System.Data;
using System.Data.Common;
using Btech.Sql.Console.Extensions;
using Btech.Sql.Console.Models.Responses.Connector;

namespace Btech.Sql.Console.Base;

public abstract class ConnectorBase : IDisposable, IAsyncDisposable
{
    private bool _disposed;

    protected ConnectorBase(string connectionString)
    {
        this.ConnectionString = connectionString;
    }

    #region Public Properties

    public abstract DbConnection Connection { get; }
    public string ConnectionString { get; }

    #endregion Public Properties

    #region Protected Methods

    private void Dispose(bool dispose)
    {
        if (!this._disposed && dispose)
        {
            this.Connection.Dispose();
            this._disposed = true;
        }
    }

    #endregion Protected Methods

    #region Abstract Methods

    public abstract DbCommand CreateCommand(string sql = null);
    protected abstract bool NeedQuotes(string postgresType);

    protected abstract string ConvertToInsertSql(
        List<(string ColumnName, bool isQuoted)> header, string table, params List<string>[] rows);

    public void Dispose()
    {
        this.Dispose(dispose: true);
        GC.SuppressFinalize(this);
    }

    #endregion Abstract Methods

    #region Public Methods

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

    public async Task OpenConnectionAsync() => await this.Connection.OpenAsync();

    public async Task CloseConnectionAsync() => await this.Connection.CloseAsync();

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