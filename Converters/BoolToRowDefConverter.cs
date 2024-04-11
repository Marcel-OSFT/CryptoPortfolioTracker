using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace CryptoPortfolioTracker.Converters
{
    public class BoolToRowDefConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter == null) return "Auto";
            string[] parameters = { "", "" };
            if (parameter != null)
            {
                parameters = (parameter as string).Split('|');
            }
            
            string width = string.Empty;
            if ( (bool)value && parameters[0].Contains("*"))
            {
                width = parameters[0];
            }
            else if ((bool)value)
            {
                //** adjust return value for selected app font
                switch (App.userPreferences.FontSize.ToString())
                {
                    case "Small":
                        {
                            width = (System.Convert.ToInt16(parameters[0])).ToString();
                            break;
                        }
                    case "Normal":
                        {
                            width = (System.Convert.ToInt16(parameters[0]) + 5).ToString();
                            break;
                        }
                    case "Large":
                        {
                            width = (System.Convert.ToInt16(parameters[0]) + 10).ToString();
                            break;
                        }
                }
            }
            return (bool)value ? width : (string)parameters[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
