using Btech.Sql.Console.Exceptions.Base;

namespace Btech.Sql.Console.Exceptions;

public class ExternalServiceException : InternalException
{
    public ExternalServiceException(Exception innerException, string serviceName, string message)
        : base(innerException, $"{serviceName} has thrown an exception. Message: '{message}'.")
    {
    }
}