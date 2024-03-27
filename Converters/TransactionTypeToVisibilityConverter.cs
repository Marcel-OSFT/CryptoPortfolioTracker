using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Converters
{
    public class TransactionTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            TransactionKind type = (TransactionKind)value;
            return type==TransactionKind.Deposit || 
                    type==TransactionKind.Withdraw || 
                    type == TransactionKind.Transfer ? "Collapsed" : "Visible";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}