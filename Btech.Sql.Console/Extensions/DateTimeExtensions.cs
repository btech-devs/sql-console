namespace Btech.Sql.Console.Extensions;

public static class DateTimeExtensions
{
    public static decimal ToElapsedTimeMs(this TimeSpan value) =>
        Math.Truncate((decimal) Math.Round(value.TotalMilliseconds, 3) * 1000m) / 1000m;
}