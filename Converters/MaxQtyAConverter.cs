﻿using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace CryptoPortfolioTracker.Converters
{
    public class MaxQtyAConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double _double = (double)value;

            //NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            string _string = "Qty (max. " + _double.ToString("G7", CultureInfo.InvariantCulture) + ")";

            return _double >= 0 ? "Qty (max. " + _double.ToString("G7", CultureInfo.InvariantCulture) + ")" : "Qty";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}