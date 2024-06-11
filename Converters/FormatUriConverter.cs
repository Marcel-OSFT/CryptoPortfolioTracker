
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace CryptoPortfolioTracker.Converters;

public class FormatUriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string result = (string)value;
        try
        {
            if ((string)value == string.Empty)
            {
                result =  App.appPath + "\\Assets\\QuestionMarkRed.png" ;
            }
        }
        catch { }

        return new Uri(result);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
    
