using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace CryptoPortfolioTracker.Converters;

public class ImageUriToBitmapImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        try
        {
            if ((string)value == string.Empty)
            {
                return new BitmapImage();
            }

            return new BitmapImage(new Uri((string)value));
        }
        catch 
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}



