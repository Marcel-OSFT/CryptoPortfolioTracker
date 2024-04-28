using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public sealed class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
            return string.Empty;

        if (parameter == null)
            return value;

        CultureInfo ci;
        if (App.userPreferences.NumberFormat.CurrencyDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
        }
        else
        {
            ci = new CultureInfo("en-US");
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
                ci = new CultureInfo("en-US");
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
