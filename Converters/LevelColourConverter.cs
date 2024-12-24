using System;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CryptoPortfolioTracker.Converters;

public class LevelColourConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var baseColor = new SolidColorBrush(Colors.White);
        var grayedOutColor = new SolidColorBrush(Colors.Gray);
        SolidColorBrush withinRangeColor;
        SolidColorBrush closeToColor;

        bool isDarkTheme = App._preferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark;

        (withinRangeColor, closeToColor) = parameter switch
        {
            "TpLevel" or "TpDist" => isDarkTheme
                ? (new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.LimeGreen))
                : (new SolidColorBrush(Colors.DarkGreen), new SolidColorBrush(Colors.LimeGreen)),
            "BuyLevel" or "BuyDist" => (new SolidColorBrush(Colors.MediumBlue), new SolidColorBrush(Colors.LightBlue)),
            "StopLevel" or "StopDist" => (new SolidColorBrush(Colors.DarkRed), new SolidColorBrush(Colors.Red)),
            _ => (baseColor, baseColor)
        };

        var WithinRangePerc = App._preferencesService.GetWithinRangePerc();
        var CloseToLevelPerc = App._preferencesService.GetCloseToPerc();

        if (value is double val)
        {
            if (double.IsInfinity(val))
            {
                return grayedOutColor;
            }
            if (val <= CloseToLevelPerc)
            {
                return closeToColor;
            }
            if (val <= WithinRangePerc)
            {
                return withinRangeColor;
            }
        }

        return baseColor;
    }


    //public object Convert(object value, Type targetType, object parameter, string language)
    //{
        
    //    var baseColor = new SolidColorBrush(Colors.White);
    //    var grayedOutColor = new SolidColorBrush(Colors.Gray);
    //    var withinRangeColor = new SolidColorBrush();
    //    var closeToColor = new SolidColorBrush();

    //    if ( App._preferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark)
    //    {      
    //        switch (parameter)
    //        {
    //            case "TpLevel": case "TpDist":
    //                {
    //                    withinRangeColor = new SolidColorBrush(Colors.Green);
    //                    closeToColor = new SolidColorBrush(Colors.LimeGreen);
    //                    break;
    //                }
    //            case "BuyLevel": case "BuyDist":
    //                {
    //                    withinRangeColor = new SolidColorBrush(Colors.MediumBlue);
    //                    closeToColor = new SolidColorBrush(Colors.LightBlue);
    //                    break;
    //                }
    //            case "StopLevel": case "StopDist":
    //                {
    //                    withinRangeColor = new SolidColorBrush(Colors.DarkRed);
    //                    closeToColor = new SolidColorBrush(Colors.Red);
    //                    break;
    //                }
    //        }
    //    }
    //    else
    //    {
    //        switch (parameter)
    //        {
    //            case "TpLevel": case "TpDist":
    //                {
    //                    withinRangeColor = new SolidColorBrush(Colors.DarkGreen);
    //                    closeToColor = new SolidColorBrush(Colors.LimeGreen);
    //                    break;
    //                }
    //            case "BuyLevel": case "BuyDist":
    //                {
    //                    withinRangeColor = new SolidColorBrush(Colors.MediumBlue);
    //                    closeToColor = new SolidColorBrush(Colors.LightBlue);
    //                    break;
    //                }
    //            case "StopLevel": case "StopDist":
    //                {
    //                    withinRangeColor = new SolidColorBrush(Colors.DarkRed);
    //                    closeToColor = new SolidColorBrush(Colors.Red);
    //                    break;
    //                }
    //        }
    //    }

    //    var WithinRangePerc = App._preferencesService.GetWithinRangePerc();
    //    var CloseToLevelPerc = App._preferencesService.GetCloseToPerc();

    //    if (value is double)
    //    {
    //        var val = (double)value;
    //        if (parameter is string Param)
    //        {
    //            if (Param.Contains("Level") & val == 0)
    //            {
    //                return grayedOutColor;
    //            }
    //        }
    //        else if (val <= CloseToLevelPerc)
    //        {
    //            return closeToColor;
    //        }
    //        else if (val <= WithinRangePerc)
    //        {
    //            return withinRangeColor;
    //        }
    //        else
    //        {
    //            return baseColor;
    //        }
    //    }
    //    else
    //    {
    //        return baseColor;
    //    }
    //}

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
