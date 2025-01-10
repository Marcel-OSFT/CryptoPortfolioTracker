using System;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public class RowDefConverterParameters
{
    public string Text { get; set; }
    public XamlRoot XamlRoot { get; set; }
}

public partial class BoolToRowDefConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter == null)
        {
            return "Auto";
        }

        double scale = 1;
        string[] paramText = { string.Empty, string.Empty };
        if (parameter is RowDefConverterParameters)
        {
            scale = ((RowDefConverterParameters)parameter).XamlRoot?.RasterizationScale ?? 1.0;
            paramText = ((RowDefConverterParameters)parameter).Text.Split('|');
        }

        var width = string.Empty;
        if ((bool)value && paramText[0].Contains('*'))
        {
            width = paramText[0];
        }
        else if ((bool)value)
        {
            
            //** adjust return value for selected app font
            switch (App._preferencesService.GetFontSize().ToString())
            {
                case "Small":
                    {
                        width = (scale * (System.Convert.ToInt16(paramText[0]) - 4)).ToString();
                        break;
                    }
                case "Normal":
                    {
                        width = (scale * System.Convert.ToInt16(paramText[0])).ToString();
                        break;
                    }
                case "Large":
                    {
                        width = (scale * (System.Convert.ToInt16(paramText[0]) + 8)).ToString();
                        break;
                    }
            }
        }
        return (bool)value ? width : (string)paramText[1];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
