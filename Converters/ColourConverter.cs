﻿using System;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CryptoPortfolioTracker.Converters;

public partial class ColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var netInvestColor = new SolidColorBrush(Colors.Goldenrod);
        var baseColor =  App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark ? 
            new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);

        var greenColor = new SolidColorBrush(Colors.ForestGreen);

        if (parameter is string param)
        {
            if (param == "NetInvestment")
            {
                return (double)value <= 0 ? netInvestColor : baseColor;
            }
        }

        if ( App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark)
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
