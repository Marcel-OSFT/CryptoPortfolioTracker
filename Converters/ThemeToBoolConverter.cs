using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace CryptoPortfolioTracker.Converters;

public class ThemeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ApplicationTheme theme)
        {
            return theme == ApplicationTheme.Dark ? true : false;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? ApplicationTheme.Dark : ApplicationTheme.Light;
    }
}