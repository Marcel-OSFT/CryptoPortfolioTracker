using System;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public class BoolToScrollModeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (bool)value ? Microsoft.UI.Xaml.Controls.ScrollMode.Disabled : Microsoft.UI.Xaml.Controls.ScrollMode.Enabled;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}