namespace Btech.Sql.Console.Base;

/// <summary>
/// Represents the base class for all controllers in project.
/// </summary>
public abstract class ControllerBase : Microsoft.AspNetCore.Mvc.ControllerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControllerBase"/> class with a logger.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    protected ControllerBase(ILogger logger)
    {
        this.Logger = logger;
    }

    #region Protected Properties

    /// <summary>
    /// Gets the logger used by the controller.
    /// </summary>
    protected ILogger Logger { get; }

    #endregion Protected Properties

    #region Protected Methods

    /// <summary>
    /// Logs a debug message using the <see cref="Logger"/>.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">An optional <see cref="Exception"/> to include in the log entry.</param>
    protected void LogDebug(string message, Exception exception = null)
    {
        this.Logger?.LogDebug(exception: exception, message: message);
    }

    /// <summary>
    /// Logs a error message using the <see cref="Logger"/>.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">An optional <see cref="Exception"/> to include in the log entry.</param>
    protected void LogError(Exception exception, string message)
    {
        this.Logger?.LogError(exception: exception, message: message);
    }

    /// <summary>
    /// Logs a information message using the <see cref="Logger"/>.
    /// </summary>
    /// <param name="message">The message to log.</param>
    protected void LogInformation(string message)
    {
        this.Logger?.LogInformation(message);
    }

    /// <summary>
    /// Logs a warning message using the <see cref="Logger"/>.
    /// </summary>
    /// <param name="message">The message to log.</param>
    protected void LogWarning(string message)
    {
        this.Logger?.LogWarning(message);
    }

    #endregion Protected Methods
}