using Btech.Core.Database.Utils;
using Btech.Sql.Console.Enums;
using ServiceCollectionExtensions = Btech.Sql.Console.Extensions.ServiceCollectionExtensions;

namespace Btech.Sql.Console.Factories;

public static class PermissionRuleFactory
{
    /// <summary>
    /// Determines if a database is the service database based on the database name and host.
    /// </summary>
    /// <param name="dbName">The name of the database.</param>
    /// <param name="dbHost">The hostname of the database.</param>
    /// <returns><c>true</c> if the database is the service database, otherwise <c>false</c>.</returns>
    private static bool IsServiceDatabase(string dbName, string dbHost)
    {
        return dbName == EnvironmentUtils.GetRequiredVariable(Core.Database.Constants.Environment.Database.Name) &&
               dbHost == EnvironmentUtils.GetRequiredVariable(Core.Database.Constants.Environment.Database.Host);
    }

    /// <summary>
    /// Filters a list of database names based on user role and database host.
    /// </summary>
    /// <param name="inDatabaseNameList">The list of database names to filter.</param>
    /// <param name="userRole">The user's role.</param>
    /// <param name="dbHost">The hostname of the database.</param>
    /// <returns>A list of filtered database names.</returns>
    public static List<string> FilterDatabaseNames(List<string> inDatabaseNameList, UserRole userRole, string dbHost)
    {
        List<string> outDatabaseNameList = inDatabaseNameList;

        if ((userRole & ~UserRole.Admin) == userRole) // not admin
        {
            if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase)
            {
                outDatabaseNameList.RemoveAll(
                    databaseName => IsServiceDatabase(databaseName, dbHost));
            }
        }

        return outDatabaseNameList;
    }

    /// <summary>
    /// Determines if a database is allowed based on user role and database host.
    /// </summary>
    /// <param name="inDatabaseName">The name of the database.</param>
    /// <param name="userRole">The user's role.</param>
    /// <param name="dbHost">The hostname of the database.</param>
    /// <returns><c>true</c> if the database is allowed, otherwise <c>false</c>.</returns>
    public static bool IsAllowedDatabase(string inDatabaseName, UserRole userRole, string dbHost)
    {
        bool isAllowed = true;

        if ((userRole & ~UserRole.Admin) == userRole) // not admin
        {
            if (ServiceCollectionExtensions.GetSessionStorageScheme() == SessionStorageScheme.RemoteDatabase &&
                IsServiceDatabase(inDatabaseName, dbHost))
            {
                isAllowed = false;
            }
        }

        return isAllowed;
    }
}