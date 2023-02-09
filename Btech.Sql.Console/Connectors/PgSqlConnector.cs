using System.Data.Common;
using Btech.Sql.Console.Base;
using Npgsql;

namespace Btech.Sql.Console.Connectors;

public class PgSqlConnector : ConnectorBase
{
    public PgSqlConnector(string connectionString) : base(connectionString)
    {
        this.Connection = new NpgsqlConnection(connectionString);
    }

    #region Override Methods

    public override NpgsqlConnection Connection { get; }

    public override DbCommand CreateCommand(string sql) =>
        new NpgsqlCommand(sql, this.Connection);

    #endregion Override Methods
}