using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace CryptoPortfolioTracker.Converters
{
    public class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string[] parameters = { "", "", "" };
            double _double = (double)value;
            if (parameter != null)
            {
                parameters = (parameter as string).Split('|');
            }

            //NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            //**   Parameter is for example......    Qty |(max.|N7|)
            string _string = (string)parameters[0] + (string)parameters[1] + _double.ToString((string)parameters[2], CultureInfo.InvariantCulture) + (string)parameters[3];
            return _double == 0 ? (string)parameters[0] : _string;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}