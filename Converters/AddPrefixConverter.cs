﻿using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoPortfolioTracker.Converters
{
    public class AddPrefixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";
            string[] parameters = { "", "", "" };
            if (parameter != null)
            {
                parameters = (parameter as string).Split('|');
            }
            string result = (string)parameters[0] + value.ToString() + (string)parameters[1];
            return result ;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
