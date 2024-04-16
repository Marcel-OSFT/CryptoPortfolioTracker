using Microsoft.UI.Xaml.Data;
using System;
using System.Diagnostics;
using System.Globalization;

namespace CryptoPortfolioTracker.Converters;

public sealed class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return null;

        if (parameter == null)
            return value;

        CultureInfo ci;
        if (App.userPreferences.NumberFormat.CurrencyDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
        }
        else
        {
            ci = new CultureInfo("en-us");
        }
        return string.Format(ci, (string)parameter, (double)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        double result;
        try
        {
            CultureInfo ci;
            if (App.userPreferences.NumberFormat.CurrencyDecimalSeparator == ",")
            {
                ci = new CultureInfo("nl-NL");
            }
            else
            {
                ci = new CultureInfo("en-us");
            }
            result = System.Convert.ToDouble((string)value, ci);
        }
        catch
        {
            result = 0;
        }
        return result;
    }
}
