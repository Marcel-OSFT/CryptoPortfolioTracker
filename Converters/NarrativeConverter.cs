using System;
using System.Diagnostics;
using System.Globalization;
using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public sealed class NarrativeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || (string)value == "- Not Assigned -")
        {
            return string.Empty;
        }

        return (string)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
