using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace CryptoPortfolioTracker.Converters
{
    public class ThemeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            SolidColorBrush brush = (SolidColorBrush)value;

            if (brush.Color == Colors.White)
            {
                return new SolidColorBrush(Colors.DarkGoldenrod); // used to override the whte color when DARK theme is choosen
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
