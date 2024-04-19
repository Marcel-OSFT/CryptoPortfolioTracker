using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters
{
    public class AddSuffixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter == null) return value;
            string[] parameters = { "", "", "" };
            parameters = (parameter as string).Split('|');

            //NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;
            string result = ((double)value).ToString((string)parameters[1], CultureInfo.InvariantCulture) + (string)parameters[0];

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
