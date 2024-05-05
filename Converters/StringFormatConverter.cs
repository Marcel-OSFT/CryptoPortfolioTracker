using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public sealed class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (parameter == null)
        {
            return value;
        }

        CultureInfo ci;
        if (App.userPreferences.NumberFormat.NumberDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
            ci.NumberFormat = App.userPreferences.NumberFormat;
        }
        else
        {
            ci = new CultureInfo("en-US");
            ci.NumberFormat = App.userPreferences.NumberFormat;
        }

        var result = string.Format(ci, (string)parameter, (double)value);
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        double result;
        try
        {
            CultureInfo ci;
            if (App.userPreferences.NumberFormat.NumberDecimalSeparator == ",")
            {
                ci = new CultureInfo("nl-NL");
                ci.NumberFormat = App.userPreferences.NumberFormat;
            }
            else
            {
                ci = new CultureInfo("en-US");
                ci.NumberFormat = App.userPreferences.NumberFormat;
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
