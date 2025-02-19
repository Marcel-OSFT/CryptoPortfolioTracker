﻿using System;
using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml.Data;

namespace CryptoPortfolioTracker.Converters;

public partial class TransactionDirectionToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (MutationDirection)value == MutationDirection.In ?
            App.AppPath + "\\Assets\\Wallet_Plus.png" :
            App.AppPath + "\\Assets\\Wallet_Minus.png";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}