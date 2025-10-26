using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters
{
    public sealed class AppThemeToggleConverter : IValueConverter
    {
        // ElementTheme -> bool (IsOn)
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ElementTheme theme)
            {
                return theme == ElementTheme.Dark;
            }
            return false;
        }

        // bool (IsOn) -> ElementTheme
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isOn)
            {
                return isOn ? ElementTheme.Dark : ElementTheme.Light;
            }
            return ElementTheme.Light;
        }
    }
}