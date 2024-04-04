using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml.Data;
using System;

namespace CryptoPortfolioTracker.Converters
{
    public class TransactionDirectionToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (MutationDirection)value == MutationDirection.In ?
                App.appPath + "\\Assets\\Wallet_Plus.png" :
                App.appPath + "\\Assets\\Wallet_Minus.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}