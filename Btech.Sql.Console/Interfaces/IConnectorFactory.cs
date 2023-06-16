using Btech.Sql.Console.Base;
using Btech.Sql.Console.Enums;

namespace Btech.Sql.Console.Interfaces;

/// <summary>
/// Represents a factory for creating instances of <see cref="ConnectorBase"/> objects.
/// </summary>
public interface IConnectorFactory
{
    /// <summary>
    /// Creates a new instance of a <see cref="ConnectorBase"/> object with the specified <paramref name="instanceType"/> and <paramref name="connectionString"/>.
    /// </summary>
    /// <param name="instanceType">The type of the instance to create.</param>
    /// <param name="connectionString">The connection string to use for the new instance.</param>
    /// <returns>A new instance of a <see cref="ConnectorBase"/> object.</returns>
    ConnectorBase Create(InstanceType instanceType, string connectionString);
}