using Microsoft.UI.Xaml.Data;
using System;
using System.Runtime.Serialization;

namespace CryptoPortfolioTracker.Converters
{
    public class TimeStampConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((DateTime)value).ToString("dd-MM-yyyy");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
