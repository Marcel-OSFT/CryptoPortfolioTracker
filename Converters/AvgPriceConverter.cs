using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using CryptoPortfolioTracker.Models;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public sealed class AvgPriceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (parameter == null)
        {
            return value;
        }

        double avgCost = 0;

        if (value is List<Asset> assets)
        {   
            double totalQty = 0;
            double totalCost = 0;
            foreach (var asset in assets)
            {
                totalQty += asset.Qty;
                totalCost += asset.Qty * asset.AverageCostPrice;
            }

            if (totalQty != 0)
            {
                avgCost = totalCost / totalQty;
            }
        }

        var ci = new CultureInfo(App._preferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App._preferencesService.GetNumberFormat();

        return string.Format(ci, (string)parameter, avgCost);
        
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        double result;
        try
        {
            
            var ci = new CultureInfo(App._preferencesService.GetAppCultureLanguage());
            ci.NumberFormat = App._preferencesService.GetNumberFormat();

            if (targetType == typeof(double))
            {
                var test = System.Convert.ToDouble((string)value, ci);
                return  System.Convert.ToDouble((string)value, ci);
            }
            else if (targetType == typeof(int))
            {
                return System.Convert.ToInt16((string)value, ci);
            }

        }
        catch
        {
            result = 0;
        }
        return 0;
    }
}
