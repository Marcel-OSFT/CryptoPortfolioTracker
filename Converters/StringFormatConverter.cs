using System;
using System.Diagnostics;
using System.Globalization;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public sealed partial class StringFormatConverter : IValueConverter
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


        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        if (value is double)
        {
            if (double.IsInfinity((double)value))
            {
                return "-";
            }
            var test = (double)value;
            var test2 = string.Format(ci, (string)parameter, (double)value);
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
            
            var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
            ci.NumberFormat = App.PreferencesService.GetNumberFormat();

            if (targetType == typeof(double))
            {
                var test = System.Convert.ToDouble((string)value, ci);
                return  System.Convert.ToDouble((string)value, ci);
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
