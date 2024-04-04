using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace CryptoPortfolioTracker.Converters
{
    public class BoolToRowDefConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter == null) return "Auto";
            
            string[] parameters = { "", "" };
            if (parameter != null)
            {
                parameters = (parameter as string).Split('|');
            }
            return (bool)value ? (string)parameters[0] : (string)parameters[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
