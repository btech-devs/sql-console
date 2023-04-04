using System.Data.Common;
using System.Text;
using Btech.Sql.Console.Base;
using Btech.Sql.Console.Extensions;
using Npgsql;

namespace Btech.Sql.Console.Connectors;

public class PgSqlConnector : ConnectorBase
{
    public PgSqlConnector(string connectionString) : base(connectionString)
    {
        this.Connection = new NpgsqlConnection(connectionString);
    }

    #region Override Methods

    protected override bool NeedQuotes(string postgresTypeName)
    {
        string[] notQuotesTypes =
        {
            "bigint",
            "bigserial",
            "integer",
            "serial",
            "smallint",
            "smallserial",
            "numeric",
            "double precision",
            "real",
            "boolean"
        };

        return !notQuotesTypes.Contains(postgresTypeName);
    }

    public override NpgsqlConnection Connection { get; }

    public override DbCommand CreateCommand(string sql) => new NpgsqlCommand(sql, this.Connection);

    protected override string ConvertToInsertSql(List<(string ColumnName, bool isQuoted)> header, string table, params List<string>[] rows)
    {
        StringBuilder query = new StringBuilder($"INSERT INTO \"{table}\" ");

        string columnNames = string.Join(',', header.Select(column => $"\"{column.ColumnName}\""));

        query.Append($"({columnNames}) VALUES ");

        query.Append(
            string.Join(
                ',',
                rows.Select(
                    columns =>
                    {
                        StringBuilder builder = new StringBuilder("(");

                        for (int columnIndex = 0; columnIndex < columns.Count; columnIndex++)
                        {
                            string value = columns[columnIndex];

                            if (value.IsNullOrEmpty())
                                value = "null";
                            else if (header[columnIndex].isQuoted)
                                value = $"\'{value}\'";

                            builder.Append(value);

                            if (columnIndex < columns.Count - 1)
                                builder.Append(',');
                        }

                        builder.Append(')');

                        return builder.ToString();
                    })));

        query.Append(';');

        return query.ToString();
    }

    #endregion Override Methods
}