using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Globalization;
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

            string withMaxQty = " (max. " + _double.ToString("G7", CultureInfo.InvariantCulture) + ")";

            return _double >= 0 
                ? loc.GetLocalizedString("TransactionDialog_QtyHeader") + withMaxQty 
                : loc.GetLocalizedString("TransactionDialog_QtyHeader");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
