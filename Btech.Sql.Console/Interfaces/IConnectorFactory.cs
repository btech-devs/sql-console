using Btech.Sql.Console.Base;
using Btech.Sql.Console.Enums;

namespace Btech.Sql.Console.Interfaces;

public interface IConnectorFactory
{
    ConnectorBase Create(InstanceType instanceType, string connectionString);
}