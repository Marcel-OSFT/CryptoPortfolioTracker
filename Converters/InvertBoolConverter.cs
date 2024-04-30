﻿using System;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}