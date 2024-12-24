using System;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Windows.UI;

namespace CryptoPortfolioTracker.Converters;

public class BackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var greenColor = new SolidColorBrush(Color.FromArgb(100, SKColors.LimeGreen.Red, SKColors.LimeGreen.Green, SKColors.LimeGreen.Blue));
        var redColor = new SolidColorBrush(Color.FromArgb(100, SKColors.OrangeRed.Red, SKColors.OrangeRed.Green, SKColors.OrangeRed.Blue));
        if (App._preferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark)
        {
            greenColor = new SolidColorBrush(Color.FromArgb(100, SKColors.DarkGreen.Red, SKColors.DarkGreen.Green, SKColors.DarkGreen.Blue));
            redColor = new SolidColorBrush(Color.FromArgb(100, SKColors.DarkRed.Red, SKColors.DarkRed.Green, SKColors.DarkRed.Blue));
        }

        return (double)value < 0 ? redColor : greenColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
