using Microsoft.UI.Xaml.Data;
using System;
using System.Diagnostics;
using System.Globalization;

namespace CryptoPortfolioTracker.Converters
{
    public sealed class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return null;

            if (parameter == null)
                return value;
            
            var ci = new CultureInfo(App.userPreferences.CultureLanguage);
            
           // return string.Format(CultureInfo.InvariantCulture, (string)parameter, (double)value);
            return string.Format(ci, (string)parameter, (double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double result;
            try
            {
                var ci = new CultureInfo(App.userPreferences.CultureLanguage);
                // result = System.Convert.ToDouble((string)value, CultureInfo.InvariantCulture);
                result = System.Convert.ToDouble((string)value, ci);
            }
            catch
            {
                result = 0;
            }
            return result;
        }
    }
}
