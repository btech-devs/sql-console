using Btech.Sql.Console.Enums;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Btech.Sql.Console.Utils;

public static class ConnectionStringBuilder
{
    public static string SetDatabase(string connectionString, string database) => $"{connectionString};Database={database}";

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