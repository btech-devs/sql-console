using Btech.Sql.Console.Exceptions.Base;

namespace Btech.Sql.Console.Exceptions;

/// <summary>
/// Exception that is thrown when an external service encounters an error.
/// </summary>
public class ExternalServiceException : InternalException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class with a specified error message and the name of the external service.
    /// </summary>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="serviceName">The name of the external service that encountered the error.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ExternalServiceException(Exception innerException, string serviceName, string message)
        : base(innerException, $"{serviceName} has thrown an exception. Message: '{message}'.")
    {
    }
}