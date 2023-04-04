using Btech.Core.Database.Utils;
using Btech.Sql.Console.Enums;
using ServiceCollectionExtensions = Btech.Sql.Console.Extensions.ServiceCollectionExtensions;

namespace Btech.Sql.Console.Factories;

public static class PermissionRuleFactory
{
    private static bool IsServiceDatabase(string dbName, string dbHost)
    {
        return dbName == EnvironmentUtils.GetRequiredVariable(Core.Database.Constants.Environment.Database.Name) &&
               dbHost == EnvironmentUtils.GetRequiredVariable(Core.Database.Constants.Environment.Database.Host);
    }

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