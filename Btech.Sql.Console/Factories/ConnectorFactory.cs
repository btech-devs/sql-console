using Btech.Sql.Console.Base;
using Btech.Sql.Console.Connectors;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Interfaces;

namespace Btech.Sql.Console.Factories;

public class ConnectorFactory : IConnectorFactory
{
    public ConnectorBase Create(InstanceType instanceType, string connectionString) =>
        instanceType switch
        {
            InstanceType.MsSql => new MsSqlConnector(connectionString),
            InstanceType.PgSql => new PgSqlConnector(connectionString),
            _ => throw new NotSupportedException($"InstanceType: '{instanceType}' is not supported.")
        };
}