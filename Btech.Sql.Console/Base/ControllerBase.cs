namespace Btech.Sql.Console.Base;

public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    protected ControllerBase(ILogger logger)
    {
        this.Logger = logger;
    }

    #region Protected Properties

    protected ILogger Logger { get; }

    #endregion Protected Properties

    #region Protected Methods

    protected void LogDebug(string message, Exception exception = null)
    {
        this.Logger?.LogDebug(exception: exception, message: message);
    }

    protected void LogError(Exception exception, string message)
    {
        this.Logger?.LogError(exception: exception, message: message);
    }

    protected void LogInformation(string message)
    {
        this.Logger?.LogInformation(message);
    }

    protected void LogWarning(string message)
    {
        this.Logger?.LogWarning(message);
    }

    #endregion Protected Methods
}