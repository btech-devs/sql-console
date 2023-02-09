using System.Data.Common;
using Btech.Sql.Console.Base;
using Microsoft.Data.SqlClient;

namespace Btech.Sql.Console.Connectors;

public class MsSqlConnector : ConnectorBase
{
    public MsSqlConnector(string connectionString) : base(connectionString)
    {
        this.Connection = new SqlConnection(connectionString);
    }

    #region Override Methods

    public override SqlConnection Connection { get; }

    public override DbCommand CreateCommand(string sql) => new SqlCommand(sql, this.Connection);

    #endregion Override Methods
}