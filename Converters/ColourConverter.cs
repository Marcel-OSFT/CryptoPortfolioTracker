using System;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CryptoPortfolioTracker.Converters;

public class ColourConverter : IValueConverter
{
    
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var greenColor = new SolidColorBrush(Colors.ForestGreen);
        if ( App._preferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark)
        {
            greenColor = new SolidColorBrush(Colors.LimeGreen);
        }

        return (double)value < 0 ? new SolidColorBrush(Colors.Red) : greenColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
