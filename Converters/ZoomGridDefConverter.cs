using System;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Services;
using CryptoPortfolioTracker.Views;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public class ZoomGridDefConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter == null || value is not FullScreenMode)
        {
            return "Auto";
        }

        string[] parameters = { "", "" };
        if (parameter is string param)
        {
            parameters = param.Split('|');
        }

        FullScreenMode zoomMode = (FullScreenMode)value;
        string defaultHeight= parameters[0];
        string view = parameters[1];
        
        switch (zoomMode)
        {
            case FullScreenMode.None:
                {
                    return defaultHeight;
                    break;
                }
            case FullScreenMode.HeatMap:
                {
                    if (view=="HeatMap+PiePortfolio" || view=="HeatMap+Graph")
                    {
                        return "1*";
                    }
                    else
                    {
                        return "0";
                    }
                    break;
                }
            case FullScreenMode.Graph:
                {
                    if (view == "Graph+PieAccounts" || view == "HeatMap+Graph")
                    {
                        return "1*";
                    }
                    else
                    {
                        return "0";
                    }
                    break;
                }
            case FullScreenMode.PiePortfolio:
                {
                    if (view == "HeatMap+PiePortfolio" || view == "PiePortfolio+PieAccounts")
                    {
                        return "1*";
                    }
                    else
                    {
                        return "0";
                    }
                    break;
                }
            case FullScreenMode.PieAccounts:
                {
                    if (view == "Graph+PieAccounts" || view == "PiePortfolio+PieAccounts")
                    {
                        return "1*";
                    }
                    else
                    {
                        return "0";
                    }
                    break;
                }
            default:
                {
                    return defaultHeight;
                    break;
                }
        }
        
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
