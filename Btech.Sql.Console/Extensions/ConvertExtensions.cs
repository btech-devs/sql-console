using System.Globalization;
using System.Text.RegularExpressions;

namespace Btech.Sql.Console.Extensions;

/// <summary>
/// Provides extension methods for converting various data types.
/// </summary>
public static class ConvertExtensions
{
    /// <summary>
    /// Converts the specified value to a nullable boolean value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>A nullable boolean value representing the converted value, or null if the conversion fails.</returns>
    public static bool? ToNullableBool<T>(this T value)
    {
        bool? result = null;

        if (value != null)
            try
            {
                string strValue = value.ToString()!;

                if (strValue == "0")
                    result = false;
                else if (strValue == "1")
                    result = true;
                else if (!Regex.IsMatch(strValue, "[0-9]+"))
                    result = !bool.TryParse(strValue, out bool res) ? Convert.ToBoolean(value) : res;
            }
            catch (Exception)
            {
                // ignored
            }

        return result;
    }

    /// <summary>
    /// Converts a <typeparamref name="T"/> value to <see cref="int"/> type value.
    /// </summary>
    /// <param name="value">A value.</param>
    /// <param name="cultureInfo">An object that supplies culture-specific formatting information.</param>
    /// <returns>An <see cref="int"/> value or null.</returns>
    public static int? ToNullableInt<T>(this T value, CultureInfo cultureInfo = null)
    {
        int? result = null;

        if (value != null)
            try
            {
                string valueStr = value.ToString()!;

                if (!valueStr.Contains('.') && !valueStr.Contains(','))
                    result = cultureInfo == null
                        ? Convert.ToInt32(value)
                        : int.Parse(valueStr, cultureInfo);
            }
            catch (Exception)
            {
                // ignored
            }

        return result;
    }
}