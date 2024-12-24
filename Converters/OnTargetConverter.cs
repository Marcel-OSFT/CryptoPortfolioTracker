using System;
using System.Collections.Generic;
using System.Diagnostics;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CryptoPortfolioTracker.Converters;

public class OnTargetConverter : IValueConverter
{


    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isDarkTheme = App._preferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark;

        SolidColorBrush atTpColor = isDarkTheme ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.DarkGreen);
        SolidColorBrush atBuyColor = isDarkTheme ? new SolidColorBrush(Colors.LightBlue) : new SolidColorBrush(Colors.Blue);
        SolidColorBrush atStopColor = isDarkTheme ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.DarkRed);
        SolidColorBrush baseColor = isDarkTheme ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);

        if (value is List<PriceLevel> levels)
        {
            foreach (var level in levels)
            {
                if (level.Status == PriceLevelStatus.TaggedPrice)
                {
                    return level.Type switch
                    {
                        PriceLevelType.TakeProfit => atTpColor,
                        PriceLevelType.Buy => atBuyColor,
                        PriceLevelType.Stop => atStopColor,
                        _ => baseColor
                    };
                }
            }
        }

        return baseColor;
    }


    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
