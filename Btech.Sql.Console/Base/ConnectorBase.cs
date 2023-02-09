using System.Data.Common;

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

    public abstract DbCommand CreateCommand(string sql);

    public void Dispose()
    {
        this.Dispose(dispose: true);
        GC.SuppressFinalize(this);
    }

    #endregion Abstract Methods

    #region Public Methods

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