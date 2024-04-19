using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Converters
{
    public class MaxQtyAConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {

            ILocalizer loc = Localizer.Get();
            double _double = (double)value;


            //NumberFormatInfo nfi = new CultureInfo("en-US", false).NumberFormat;

            var maxQty = _double.ToString("G7", CultureInfo.InvariantCulture);
            var length = maxQty.Length;
            if (length > 8) length = 9;

            string finalString = " (max " + maxQty.Substring(0,length) + ")";

            return _double >= 0
                ? loc.GetLocalizedString("TransactionDialog_QtyHeader") + finalString
                : loc.GetLocalizedString("TransactionDialog_QtyHeader");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
