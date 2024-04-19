using System;
using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters
{
    public class TransactionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            TransactionKind type = (TransactionKind)value;
            return type == TransactionKind.Deposit ||
                    type == TransactionKind.Withdraw ||
                    type == TransactionKind.Transfer ? "Collapsed" : "Visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}