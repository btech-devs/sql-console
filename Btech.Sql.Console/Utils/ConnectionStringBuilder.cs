using Btech.Sql.Console.Enums;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Btech.Sql.Console.Utils;

public static class ConnectionStringBuilder
{
    public static string SetDatabase(string connectionString, string database) => $"{connectionString};Database={database}";

    public static string SetupCommandTimeout(InstanceType instanceType, string connectionString, int commandTimeout = 60) =>
        instanceType switch
        {
            InstanceType.MsSql => $"{connectionString};Command Timeout={commandTimeout}",
            InstanceType.PgSql => $"{connectionString};CommandTimeout={commandTimeout}",
            _ => throw new NotSupportedException($"InstanceType: '{instanceType}' is not supported.")
        };

    public static string SetupTimeout(InstanceType instanceType, string connectionString, int timeout = 60) =>
        instanceType switch
        {
            InstanceType.MsSql => $"{connectionString};Connect Timeout={timeout}",
            InstanceType.PgSql => $"{connectionString};Timeout={timeout}",
            _ => throw new NotSupportedException($"InstanceType: '{instanceType}' is not supported.")
        };

    public static string CreateConnectionString(
        InstanceType instanceType, string host, int port, string username, string password) =>
        instanceType switch
        {
            InstanceType.PgSql => new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Username = username,
                Password = password
            }.ConnectionString,
            InstanceType.MsSql => new SqlConnectionStringBuilder
            {
                ["server"] = $"{host},{port}",
                ["TrustServerCertificate"] = "True",
                UserID = username,
                Password = password
            }.ConnectionString,
            _ => throw new NotSupportedException($"InstanceType: '{instanceType}' is not supported.")
        };
}