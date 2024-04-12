using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml.Data;
using System;
using Microsoft.Windows.ApplicationModel.Resources;

namespace CryptoPortfolioTracker.Converters;

public class TransactionTypeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return ((TransactionKind)value).AsDisplayString();

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}