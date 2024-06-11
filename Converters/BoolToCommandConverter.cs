
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows.Input;

namespace CryptoPortfolioTracker.Converters;

public class BoolToCommandConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var test = (ICommand)parameter;
        if (test is not null)
        {
            Debug.WriteLine("");
        }
        return (bool)value ? null : (ICommand)parameter;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

