using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
