using System;
using System.Globalization;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Converters;

public partial class MaxQtyAConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var loc = Localizer.Get();
        var _double = (double)value;

        CultureInfo ci;
        if (App._preferencesService.GetNumberFormat().NumberDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
            ci.NumberFormat = App._preferencesService.GetNumberFormat();
        }
        else
        {
            ci = new CultureInfo("en-us");
            ci.NumberFormat = App._preferencesService.GetNumberFormat();
        }

        var maxQty = _double.ToString("0.########", ci);
        var length = maxQty.Length;
        if (length > 9)
        {
            length = 10;
        }

        var finalString = " (max " + maxQty.Substring(0,length) + ")";
        return _double >= 0
            ? loc.GetLocalizedString("TransactionDialog_QtyHeader") + finalString
            : loc.GetLocalizedString("TransactionDialog_QtyHeader");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
