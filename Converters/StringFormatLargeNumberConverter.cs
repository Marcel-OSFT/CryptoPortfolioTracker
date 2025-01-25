using System;
using System.Diagnostics;
using System.Globalization;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public sealed partial class StringFormatLargeNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return "-";
        }

        if (parameter == null)
        {
            return value;
        }


        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        if (value is double || value is int)
        {
            if (double.IsInfinity((double)value))
            {
                return "-";
            }

            var number = (double)value;

            if (number >= 1_000_000_000)
            {
                return string.Format(ci, (string)parameter, (double)number / 1_000_000_000D) + "B";
                //return (number / 1_000_000_000D).ToString("$ 0.#") + "B";
            }
            if (number >= 1_000_000)
            {
                return string.Format(ci, (string)parameter, (double)number / 1_000_000D) + "M";
               // return (number / 1_000_000D).ToString("$ 0.#") + "M";
            }
            if (number >= 1_000)
            {
                return string.Format(ci, (string)parameter, (double)number / 1_000D) + "K";
                //return (number / 1_000D).ToString("$ 0.#") + "K";
            }

            //return string.Format(ci, (string)parameter, (double)value);
        }
        //else if (value is int)
        //{
        //    return string.Format(ci, (string)parameter, (int)value);
        //}

        return "-";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
