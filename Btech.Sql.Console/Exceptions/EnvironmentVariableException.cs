using Btech.Sql.Console.Exceptions.Base;

namespace Btech.Sql.Console.Exceptions;

public class EnvironmentVariableException : InternalException
{
    public EnvironmentVariableException(Exception inner, string environmentVariableName)
        : base(inner, $"Invalid value provided: '{environmentVariableName}'. Please check environment settings.")
    {
    }

    public EnvironmentVariableException(string environmentVariableName)
        : base($"Invalid value provided: '{environmentVariableName}'. Please check environment settings.")
    {
    }

    public EnvironmentVariableException(params string[] requiredEnvironmentVariableNames)
        : base($"At least one setting from the following should be provided: '{string.Join(',', requiredEnvironmentVariableNames)}'. Please check environment settings.")
    {
    }
}