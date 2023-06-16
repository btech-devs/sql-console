namespace Btech.Sql.Console.Extensions;

/// <summary>
/// Provides extension methods for DateTime.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Calculates the elapsed time in milliseconds for the given TimeSpan value.
    /// </summary>
    /// <param name="value">The TimeSpan value for which to calculate the elapsed time in milliseconds.</param>
    /// <returns>The elapsed time in milliseconds, truncated to three decimal places.</returns>
    public static decimal ToElapsedTimeMs(this TimeSpan value) =>
        Math.Truncate((decimal) Math.Round(value.TotalMilliseconds, 3) * 1000m) / 1000m;
}