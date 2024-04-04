using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;

namespace CryptoPortfolioTracker.Converters
{
    public class ListCountToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            List<string> list = (List<string>)value;
            bool result = false;
            if (list != null)
            {
                result = list.Count > 0;
            }
            return result ? "Select ..." : "Nothing available";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
