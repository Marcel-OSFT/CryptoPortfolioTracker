using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using Windows.UI;


using TemperatureMonitor.Enums;
using TemperatureMonitor.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Shell;
using WinUI3Localizer;


namespace TemperatureMonitor.Converters;

public class Functions
{
    private static readonly Settings? _settings = App.Container.GetService<Settings>();

    public static Visibility TrueToVisible(bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }
    public static Visibility TrueToVisible(bool? value)
    {
        return value is not null && value == true ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility FalseToVisible(bool value)
    {
        return value ? Visibility.Collapsed : Visibility.Visible;
    }

    public static bool InvertBool(bool value)
    {
        return !value;
    }
    public static bool InvertBool(bool? value)
    {
        if (value is null) return false;

        return !(bool)value;
    }
    public static ScrollMode TrueToScrollModeDisabled(bool value)
    {
        return (bool)value ? Microsoft.UI.Xaml.Controls.ScrollMode.Disabled : Microsoft.UI.Xaml.Controls.ScrollMode.Enabled;
    }
    public static Uri FormatUri(string value)
    {
        string result = string.Empty;
        try
        {
            if (value != string.Empty)
            {
                var uriWithoutQuery = ((string)value).Split('?')[0];
                var fileName = Path.GetFileName(uriWithoutQuery);
                string iconPath;

                if (fileName != "QuestionMarkBlue.png")
                {
                    iconPath = Path.Combine(AppConstants.IconsPath, fileName);
                }
                else
                {
                    iconPath = Path.Combine(AppConstants.AppPath, "Assets", fileName);
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
                result = AppConstants.AppPath + "\\Assets\\QuestionMarkRed.png";
            }
        }
        catch
        {
            //do nothing
        }
        return new Uri(result);
    }
    
    public static string FormatValueToString(double value, string format)
    {
        if (format == null || format == string.Empty)
        {
            return value.ToString();
        }

        var ci = new CultureInfo(_settings.AppCultureLanguage);
        ci.NumberFormat = _settings.NumberFormat;
        
        if (double.IsInfinity((double)value))
        {
            return "-";
        }

        if (value is double number && format == " $ {0:0.########}")
        {
            long integerPartLength = Math.Abs((long)number).ToString().Length;
            int decimalPlaces = Math.Max(0, 9 - (int)integerPartLength);
            decimalPlaces = decimalPlaces == 1 ? 2 : decimalPlaces;
            format =  "$ {0:F" + decimalPlaces.ToString() + "}";
            // '$ {0:F5}'
        }
        return string.Format(ci, format, value);
    }

    


    public static SolidColorBrush DoubleToColour(double value, string parameter = "")
    {
        var netInvestColor = new SolidColorBrush(Colors.Goldenrod);
       // var baseColor = Settings.AppTheme == Microsoft.UI.Xaml.ElementTheme.Dark ?
        var baseColor = _settings.AppTheme == Microsoft.UI.Xaml.ElementTheme.Dark ?
            new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);

        var greenColor = new SolidColorBrush(Colors.ForestGreen);
        
        if (parameter == "NetInvestment")
        {
            return (double)value <= 0 ? netInvestColor : baseColor;
        }

        //if (Settings.AppTheme == Microsoft.UI.Xaml.ElementTheme.Dark)
        if (_settings.AppTheme == Microsoft.UI.Xaml.ElementTheme.Dark)
        {
            greenColor = new SolidColorBrush(Colors.LimeGreen);
        }
        return (double)value < 0 ? new SolidColorBrush(Colors.Red) : greenColor;
    }
    public Int32 FontSizeToImageSize(double value)
    {
        switch (value)
        {
            case 14:
                {
                    return 25;
                }
            case 16:
                {
                    return 32;
                }
            case 18:
                {
                    return 40;
                }
            default: return 32;
        }
    }
   
    public static string DateTimeToString(DateTime value)
    {
        return value.ToString("dd-MM-yyyy");
    }
    public static string AddPrefixAndSuffixToString(string value, string prefix, string suffix)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }
        return prefix + value.ToString() + suffix;
    }
    public static string AddPrefixAndSuffixToString(double value, string prefix, string suffix)
    {
        return prefix + value.ToString() + suffix;
    }
    public static string FormatDoubleAndAddSuffix(double value, string format, string suffix)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return value.ToString() + suffix;
        }
        return value.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }
    public static Visibility ZeroToCollapsed(double value)
    {
        return value == 0 ? Visibility.Collapsed : Visibility.Visible;
    }
    
    public static string SetNotAssignedToEmpty(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "- Not Assigned -")
        {
            return string.Empty;
        }

        return value;
    }
    public static string Int32ToString(Int32 value, string format)
    {
        if (format == null || format == string.Empty)
        {
            return value.ToString();
        }

        var ci = new CultureInfo(_settings.AppCultureLanguage);
        ci.NumberFormat = _settings.NumberFormat;

        if (double.IsInfinity((double)value))
        {
            return "-";
        }
        return string.Format(ci, format, value);
    }
   
    public static double TrueToOpacityOne(bool value)
    {
        return (bool)value ? 1.0 : 0.2;
    }
    public static string FormatMaxQtyA(double value)
    {
        var loc = Localizer.Get();
        var _double = (double)value;

        CultureInfo ci;
        if (_settings.NumberFormat.NumberDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
            ci.NumberFormat = _settings.NumberFormat;
        }
        else
        {
            ci = new CultureInfo("en-US");
            ci.NumberFormat = _settings.NumberFormat;
        }

        var maxQty = _double.ToString("0.########", ci);
        var length = maxQty.Length;
        if (length > 9)
        {
            length = 10;
        }

        var finalString = " (max " + maxQty.Substring(0, length) + ")";
        return _double >= 0
            ? loc.GetLocalizedString("TransactionDialog_QtyHeader") + finalString
            : loc.GetLocalizedString("TransactionDialog_QtyHeader");
    }
    public static GridLength BoolToRowDef(bool value, string trueRowDef, string falseRowDef, XamlRoot root)
    {
        if (string.IsNullOrWhiteSpace(trueRowDef) || string.IsNullOrWhiteSpace(falseRowDef))
        {
            return GridLength.Auto;
        }

        double scale = root?.RasterizationScale ?? 1.0;

        double width = 0;

        if (value && trueRowDef.Contains('*'))
        {
            var rowDef = Convert.ToDouble(trueRowDef.Replace("*", ""));
            return new GridLength(rowDef, GridUnitType.Star);
        }
        else if (value)
        {
            //** adjust return value for selected app font
            switch (_settings.FontSize.ToString())
            {
                case "Small":
                    {
                        width = scale * (Convert.ToInt16(trueRowDef) - 4);
                        break;
                    }
                case "Normal":
                    {
                        width = scale * Convert.ToInt16(trueRowDef);
                        break;
                    }
                case "Large":
                    {
                        width = scale * (Convert.ToInt16(trueRowDef) + 8);
                        break;
                    }
            }
            return new GridLength(width);
        }

        if (falseRowDef.Contains('*'))
        {
            return new GridLength(Convert.ToDouble(falseRowDef.Replace("*", "")), GridUnitType.Star);
        }
        else
        {
            return new GridLength(Convert.ToDouble(falseRowDef));
        }
    }
   
    
}





