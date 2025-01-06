using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System;
using System.Diagnostics;
using System.IO;
using Windows.Storage;

namespace CryptoPortfolioTracker.Converters;

public partial class FormatUriConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string result = string.Empty;
        try
        {
            if ((string)value != string.Empty)
            {
                var uriWithoutQuery = ((string)value).Split('?')[0];
                var fileName = Path.GetFileName(uriWithoutQuery);
                string iconPath;

                if (fileName != "QuestionMarkBlue.png")
                {
                    iconPath = App.appDataPath + "\\" + App.IconsFolder + "\\" + fileName;
                }
                else
                {
                    iconPath = App.appPath + "\\Assets\\" + fileName;
                }
                

                //*** get cached icon
                if (File.Exists(iconPath))
                {
                    result = iconPath;
                }
                else
                {
                    result = (string)value;
                }
            }
            else
            {
                result = App.appPath + "\\Assets\\QuestionMarkRed.png";
            }
        }
        catch
        { 
           //do nothing
        }
        return new Uri(result);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
    
