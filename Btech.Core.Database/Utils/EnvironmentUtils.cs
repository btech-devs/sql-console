using System;

namespace Btech.Core.Database.Utils;

public static class EnvironmentUtils
{
    public static string GetRequiredVariable(string variableName)
    {
        string variableValue = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(variableValue))
        {
            throw new ApplicationException($"'{variableName}' required environment variable does not exist.");
        }

        return variableValue;
    }
}