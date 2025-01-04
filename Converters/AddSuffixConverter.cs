using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public partial class AddSuffixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter == null)
        {
            return value;
        }
        string[] parameters = { "", "", "" };
        if (parameter is string param) { parameters = param.Split('|'); }
        var result = ((double)value).ToString((string)parameters[1], CultureInfo.InvariantCulture) + (string)parameters[0];
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
