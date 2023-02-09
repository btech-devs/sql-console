using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Btech.Core.Database.Extensions;

public static class ConvertExtensions
{
    public static decimal? ToNullableDecimal<T>(this T value, CultureInfo cultureInfo = null)
    {
        decimal? result = null;

        if (value != null)
            try
            {
                result = cultureInfo == null
                    ? Convert.ToDecimal(value)
                    : decimal.Parse(value.ToString()!, cultureInfo);
            }
            catch (Exception)
            {
                // ignored
            }

        return result;
    }

    public static int? ToNullableInt<T>(this T value, CultureInfo cultureInfo = null)
    {
        int? result = null;

        if (value != null)
            try
            {
                string valueStr = value.ToString()!;

                if (valueStr.Contains('.') || valueStr.Contains(','))
                    result = (int?) value.ToNullableDecimal(cultureInfo);
                else
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
}