﻿using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace CryptoPortfolioTracker.Converters
{
    public class FontSizeToImageSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is double)) return 32; //return at least a valid value

            switch ((double)value)
            {
                case 14:
                    {
                        return 25;
                    }
                case 16:
                    {
                        return 32;
                    }
                case 18:
                    {
                        return 40;
                    }
                default: return 32;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}