namespace Btech.Sql.Console.Exceptions.Base;

public abstract class InternalException : Exception
{
    protected InternalException(
        Exception innerException,
        string message) : base(message, innerException)
    {
    }

    protected InternalException(string message) : base(message)
    {
    }
}