using Btech.Sql.Console.Base;
using Btech.Sql.Console.Connectors;
using Btech.Sql.Console.Enums;
using Btech.Sql.Console.Interfaces;

namespace Btech.Sql.Console.Factories;

/// <summary>
/// Factory class that creates an instance of <see cref="ConnectorBase"/> based on the <see cref="InstanceType"/>.
/// </summary>
public class ConnectorFactory : IConnectorFactory
{
    /// <summary>
    /// Creates an instance of <see cref="ConnectorBase"/> based on the specified <paramref name="instanceType"/> and <paramref name="connectionString"/>.
    /// </summary>
    /// <param name="instanceType">The type of database instance.</param>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>An instance of <see cref="ConnectorBase"/>.</returns>
    /// <exception cref="NotSupportedException">Thrown when the specified <paramref name="instanceType"/> is not supported.</exception>
    public ConnectorBase Create(InstanceType instanceType, string connectionString) =>
        instanceType switch
        {
            InstanceType.MsSql => new MsSqlConnector(connectionString),
            InstanceType.PgSql => new PgSqlConnector(connectionString),
            _ => throw new NotSupportedException($"InstanceType: '{instanceType}' is not supported.")
        };
}