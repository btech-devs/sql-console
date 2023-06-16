using Btech.Sql.Console.Exceptions.Base;

namespace Btech.Sql.Console.Exceptions;

/// <summary>
/// Exception thrown when an invalid or missing environment variable is encountered.
/// </summary>
public class EnvironmentVariableException : InternalException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableException"/> class with the specified inner exception and environment variable name.
    /// </summary>
    /// <param name="inner">The inner exception.</param>
    /// <param name="environmentVariableName">The name of the environment variable.</param>
    public EnvironmentVariableException(Exception inner, string environmentVariableName)
        : base(inner, $"Invalid value provided: '{environmentVariableName}'. Please check environment settings.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableException"/> class with the specified environment variable name.
    /// </summary>
    /// <param name="environmentVariableName">The name of the environment variable.</param>
    public EnvironmentVariableException(string environmentVariableName)
        : base($"Invalid value provided: '{environmentVariableName}'. Please check environment settings.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentVariableException"/> class with the specified required environment variable names.
    /// </summary>
    /// <param name="requiredEnvironmentVariableNames">The names of the required environment variables.</param>
    public EnvironmentVariableException(params string[] requiredEnvironmentVariableNames)
        : base($"At least one setting from the following should be provided: '{string.Join(',', requiredEnvironmentVariableNames)}'. Please check environment settings.")
    {
    }
}