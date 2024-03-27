using LanguageExt.Common;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            //return string.Format(nfi, (string)parameter,(double)value);
            return string.Format(CultureInfo.InvariantCulture, (string)parameter,(double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            double result;
            try
            {
                result = System.Convert.ToDouble((string)value, CultureInfo.InvariantCulture);
            }
            catch 
            {
                result = 0;
            }
            return result;
        }
    }
}
