using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace CryptoPortfolioTracker.Converters;

public class ThemeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ElementTheme theme)
        {
            return theme == ElementTheme.Dark ? true : false;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? ElementTheme.Dark : ElementTheme.Light;
    }
}