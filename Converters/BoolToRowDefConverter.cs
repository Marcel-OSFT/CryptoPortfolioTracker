using System;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public class BoolToRowDefConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter == null)
        {
            return "Auto";
        }

        string[] parameters = { "", "" };
        if (parameter is string param)
        {
            parameters = param.Split('|');
        }

        var width = string.Empty;
        if ((bool)value && parameters[0].Contains('*'))
        {
            width = parameters[0];
        }
        else if ((bool)value)
        {
            var scale = MainPage.Current.XamlRoot.RasterizationScale;
            //** adjust return value for selected app font
            switch (App._preferencesService.GetFontSize().ToString())
            {
                case "Small":
                    {
                        width = (scale * (System.Convert.ToInt16(parameters[0]) - 4)).ToString();
                        break;
                    }
                case "Normal":
                    {
                        width = (scale * System.Convert.ToInt16(parameters[0])).ToString();
                        break;
                    }
                case "Large":
                    {
                        width = (scale * (System.Convert.ToInt16(parameters[0]) + 8)).ToString();
                        break;
                    }
            }
        }
        return (bool)value ? width : (string)parameters[1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
