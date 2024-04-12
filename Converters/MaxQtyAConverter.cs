using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Globalization;

namespace CryptoPortfolioTracker.Converters
{
    public class MaxQtyAConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double _double = (double)value;

            ResourceLoader rl = new ResourceLoader();

            //NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            string withMaxQty = " (max. " + _double.ToString("G7", CultureInfo.InvariantCulture) + ")";

            return _double >= 0 
                ? rl.GetString("TransactionDialog_QtyHeader") + withMaxQty 
                : rl.GetString("TransactionDialog_QtyHeader");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
