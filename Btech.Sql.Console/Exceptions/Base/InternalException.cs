namespace Btech.Sql.Console.Exceptions.Base;

/// <summary>
/// Represents an exception that occurred within the application code.
/// </summary>
public abstract class InternalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InternalException"/> class with a specified error message and a reference to the inner exception that caused the exception.
    /// </summary>
    /// <param name="innerException">The exception that caused the current exception.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    protected InternalException(Exception innerException, string message) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    protected InternalException(string message) : base(message)
    {
    }
}