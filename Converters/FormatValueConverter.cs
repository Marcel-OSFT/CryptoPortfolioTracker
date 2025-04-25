using System;
using System.Diagnostics;
using System.Globalization;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;


namespace CryptoPortfolioTracker.Converters;

public class FormatValueToString : IValueConverter
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
        if (App.PreferencesService.GetNumberFormat().NumberDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
            ci.NumberFormat = App.PreferencesService.GetNumberFormat();
        }
        else
        {
            ci = new CultureInfo("en-US");
            ci.NumberFormat = App.PreferencesService.GetNumberFormat();
        }

        if (value is double)
        {
            return string.Format(ci, (string)parameter, (double)value);
        }
        else if (value is int)
        {
            return string.Format(ci, (string)parameter, (int)value);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        double result;
        try
        {
            CultureInfo ci;
            if (App.PreferencesService.GetNumberFormat().NumberDecimalSeparator == ",")
            {
                ci = new CultureInfo("nl-NL");
                ci.NumberFormat = App.PreferencesService.GetNumberFormat();
            }
            else
            {
                ci = new CultureInfo("en-US");
                ci.NumberFormat = App.PreferencesService.GetNumberFormat();
            }

            if (targetType == typeof(double))
            {
                return System.Convert.ToDouble((string)value, ci);
            }
            else if (targetType == typeof(int))
            {
                return System.Convert.ToInt16((string)value, ci);
            }

        }
        catch
        {
            result = 0;
        }
        return 0;
    }

}