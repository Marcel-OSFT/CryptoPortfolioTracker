using System;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Uri = System.Uri;

namespace CryptoPortfolioTracker.Converters;

public partial class ImageUriToBitmapImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is string && (string)value != string.Empty ? value : new Uri(App.appPath + "\\Assets\\QuestionMarkRed.png");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}



