
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.Shell;
using WinUI3Localizer;

namespace CryptoPortfolioTracker.Converters;

public static class Functions
{
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
                    iconPath = App.AppDataPath + "\\" + App.IconsFolder + "\\" + fileName;
                }
                else
                {
                    iconPath = App.AppPath + "\\Assets\\" + fileName;
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
                result = App.AppPath + "\\Assets\\QuestionMarkRed.png";
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

        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        var isValueAsterix = App.PreferencesService.GetAreValesMasked();

        if (isValueAsterix && !format.Contains("%"))
        {
            return "********";
        }
        
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
        var baseColor = App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark ?
            new SolidColorBrush(Colors.White)
            : new SolidColorBrush(Colors.Black);

        var greenColor = new SolidColorBrush(Colors.ForestGreen);
        
        if (parameter == "NetInvestment")
        {
            return (double)value <= 0 ? netInvestColor : baseColor;
        }

        if (App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark)
        {
            greenColor = new SolidColorBrush(Colors.LimeGreen);
        }
        return (double)value < 0 ? new SolidColorBrush(Colors.Red) : greenColor;
    }
    public static Int32 FontSizeToImageSize(double value)
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
    public static ImageSource StringToImageSource(string value)
    {
        return new BitmapImage(new Uri(string.IsNullOrWhiteSpace(value) ? App.AppPath + "\\Assets\\QuestionMarkRed.png" : value));
    }
    public static ImageSource TransactionDirectionToBitmapImage(MutationDirection value)
    {
        var imagePath = (MutationDirection)value == MutationDirection.In ?
            App.AppPath + "\\Assets\\Wallet_Plus.png" :
            App.AppPath + "\\Assets\\Wallet_Minus.png";
        return new BitmapImage(new Uri(imagePath));
    }
    public static string TransactionKindToString(TransactionKind value)
    {
        return ((TransactionKind)value).AsDisplayString();

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
    public static Visibility TransactionKindToVisibility(TransactionKind value)
    {
        return value == TransactionKind.Deposit ||
                value == TransactionKind.Withdraw ||
                value == TransactionKind.Transfer ? Visibility.Collapsed : Visibility.Visible;
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

        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        var isValueAsterix = App.PreferencesService.GetAreValesMasked();

        if (isValueAsterix)
        {
            return "********";
        }

        if (double.IsInfinity((double)value))
        {
            return "-";
        }
        return string.Format(ci, format, value);
    }
    public static SolidColorBrush PriceLevelTypeToColor(ICollection<PriceLevel> value)
    {
        bool isDarkTheme = App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark;

        SolidColorBrush atTpColor = isDarkTheme ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.DarkGreen);
        SolidColorBrush atBuyColor = isDarkTheme ? new SolidColorBrush(Colors.LightBlue) : new SolidColorBrush(Colors.Blue);
        SolidColorBrush atStopColor = isDarkTheme ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.DarkRed);
        SolidColorBrush baseColor = isDarkTheme ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black);

        if (value is List<PriceLevel> levels)
        {
            foreach (var level in levels)
            {
                if (level.Status == PriceLevelStatus.TaggedPrice)
                {
                    return level.Type switch
                    {
                        PriceLevelType.TakeProfit => atTpColor,
                        PriceLevelType.Buy => atBuyColor,
                        PriceLevelType.Stop => atStopColor,
                        _ => baseColor
                    };
                }
            }
        }

        return baseColor;
    }
    public static string GetAndFormatAssetAverageCost(ICollection<Asset> value, string parameter)
    {
        double avgCost = 0;

        if (value is List<Asset> assets && assets.Any())
        {
            double totalQty = 0;
            double totalCost = 0;
            foreach (var asset in assets)
            {
                totalQty += asset.Qty;
                totalCost += asset.Qty * asset.AverageCostPrice;
            }

            if (totalQty != 0)
            {
                avgCost = totalCost / totalQty;
            }
        }
        else
        {
            return string.Empty;
        }

        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        if (!string.IsNullOrWhiteSpace(parameter))
        {
            return string.Format(ci, parameter, avgCost);
        }
        else
        {
            return avgCost.ToString();
        }

    }

    public static ICollection<Coin> FilterAssetCoins(ICollection<Coin> value)
    {
        //*** TotalValue is taken into account to avoid rare layoutCycle error in case of Coin is an Asset (has mutations) but TotalValue of Narrative is 0.
        ICollection<Coin> assetCoins = new List<Coin>();

        if (value is null || !value.Any()) return assetCoins; 
        
        foreach (var coin in value)
        {
            if (coin.IsAsset)
            {
                assetCoins.Add(coin);
            }
        }
        return assetCoins;
    }

    public static SolidColorBrush PriceLevelToColor(ICollection<PriceLevel> value1, int index, string parameter)
    {
        var baseColor = new SolidColorBrush(Colors.White);
        var grayedOutColor = new SolidColorBrush(Colors.Gray);
        SolidColorBrush withinRangeColor;
        SolidColorBrush closeToColor;

        if (index > value1.Count - 1) return grayedOutColor;
        var value = (value1 as List<PriceLevel>)[index].DistanceToValuePerc;

        bool isDarkTheme = App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark;

        (withinRangeColor, closeToColor) = parameter switch
        {
            "TpLevel" or "TpDist" => isDarkTheme
                ? (new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.LimeGreen))
                : (new SolidColorBrush(Colors.DarkGreen), new SolidColorBrush(Colors.LimeGreen)),
            "BuyLevel" or "BuyDist" => (new SolidColorBrush(Colors.MediumBlue), new SolidColorBrush(Colors.LightBlue)),
            "StopLevel" or "StopDist" => (new SolidColorBrush(Colors.DarkRed), new SolidColorBrush(Colors.Red)),
            "EmaLevel" or "EmaDist" => (new SolidColorBrush(Colors.DarkRed), new SolidColorBrush(Colors.Red)),
            _ => (baseColor, baseColor)
        };

        var WithinRangePerc = App.PreferencesService.GetWithinRangePerc();
        var CloseToLevelPerc = App.PreferencesService.GetCloseToPerc();

        if (index < 3 && value is double val)
        {
            if (double.IsInfinity(val))
            {
                return grayedOutColor;
            }
            if (val >= (-1 * CloseToLevelPerc))
            {
                return closeToColor;
            }
            if (val >= (-1 * WithinRangePerc))
            {
                return withinRangeColor;
            }
        }
        else if (index == 3 && value is double val2 )
        {
            if (double.IsInfinity(val2))
            {
                return grayedOutColor;
            }
            if (val2 <= (-1 * CloseToLevelPerc))
            {
                return closeToColor;
            }
            if (val2 <= (-1 * WithinRangePerc))
            {
                return withinRangeColor;
            }
        }

        return baseColor;
    }
    public static string FormatDistanceToLevelToString(ICollection<PriceLevel> value1, int index, string format)
    {
        if (index > value1.Count - 1) return "-";
        var value = (value1.ElementAt(index) as PriceLevel).DistanceToValuePerc;
        
        if (format == null || format == string.Empty)
        {
            return value.ToString();
        }

        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        var isValueAsterix = App.PreferencesService.GetAreValesMasked();

        if (isValueAsterix)
        {
            return "********";
        }

        if (double.IsInfinity((double)value))
        {
            return "-";
        }
        return string.Format(ci, format, value);
    }
    public static string FormatPriceLevelToString(ICollection<PriceLevel> value1, int index, string format)
    {
        if (index > value1.Count - 1) return "-";

        var value = (value1.ElementAt(index) as PriceLevel).Value;


        if (format == null || format == string.Empty)
        {
            return value.ToString();
        }

        var ci = new CultureInfo(App.PreferencesService.GetAppCultureLanguage());
        ci.NumberFormat = App.PreferencesService.GetNumberFormat();

        var isValueAsterix = App.PreferencesService.GetAreValesMasked();

        if (isValueAsterix)
        {
            return "********";
        }

        if (double.IsInfinity((double)value))
        {
            return "-";
        }

        if (value is double number && format == "$ {0:0.########}" && number > 0)
        {
            long integerPartLength = Math.Abs((long)number).ToString().Length;
            int decimalPlaces = Math.Max(0, 9 - (int)integerPartLength);
            decimalPlaces = decimalPlaces == 1 ? 2 : decimalPlaces;
            format = "$ {0:F" + decimalPlaces.ToString() + "}";
            // '$ {0:F5}'
        }
        return string.Format(ci, format, value);
    }
    public static SolidColorBrush ValueToRedOrGreenBackground(double value)
    {
        var greenColor = new SolidColorBrush(Color.FromArgb(100, SKColors.LimeGreen.Red, SKColors.LimeGreen.Green, SKColors.LimeGreen.Blue));
        var redColor = new SolidColorBrush(Color.FromArgb(100, SKColors.OrangeRed.Red, SKColors.OrangeRed.Green, SKColors.OrangeRed.Blue));
        if (App.PreferencesService.GetAppTheme() == Microsoft.UI.Xaml.ElementTheme.Dark)
        {
            greenColor = new SolidColorBrush(Color.FromArgb(100, SKColors.DarkGreen.Red, SKColors.DarkGreen.Green, SKColors.DarkGreen.Blue));
            redColor = new SolidColorBrush(Color.FromArgb(100, SKColors.DarkRed.Red, SKColors.DarkRed.Green, SKColors.DarkRed.Blue));
        }

        return (double)value < 0 ? redColor : greenColor;
    }
    public static bool DarkThemeToTrue(ElementTheme value)
    {
        if (value is ElementTheme theme)
        {
            return theme == ElementTheme.Dark ? true : false;
        }
        return false;
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
        if (App.PreferencesService.GetNumberFormat().NumberDecimalSeparator == ",")
        {
            ci = new CultureInfo("nl-NL");
            ci.NumberFormat = App.PreferencesService.GetNumberFormat();
        }
        else
        {
            ci = new CultureInfo("en-US");
            ci.NumberFormat = App.PreferencesService.GetNumberFormat();
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
            switch (App.PreferencesService.GetFontSize().ToString())
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
    public static GridLength FullScreenToGridDef(FullScreenMode value, string parameter, string elements)
    {
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return GridLength.Auto;
        }
        
        GridLength gridDef;
        if (parameter.Contains('*'))
        {
            gridDef = new GridLength(Convert.ToDouble(parameter.Replace("*", "")), GridUnitType.Star);
        }
        else
        {
            gridDef = new GridLength(Convert.ToDouble(parameter));
        }

        switch (value)
        {
            case FullScreenMode.None:
                {
                    return gridDef;
                }
            case FullScreenMode.HeatMap:
                {
                    if (elements == "HeatMap+PiePortfolio" || elements == "HeatMap+Graph")
                    {
                        return new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        return new GridLength(0);
                    }
                }
            case FullScreenMode.Graph:
                {
                    if (elements == "Graph+PieAccounts" || elements == "HeatMap+Graph")
                    {
                        return new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        return new GridLength(0);
                    }
                }
            case FullScreenMode.PiePortfolio:
                {
                    if (elements == "HeatMap+PiePortfolio" || elements == "PiePortfolio+PieAccounts")
                    {
                        return new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        return new GridLength(0);
                    }
                }
            case FullScreenMode.PieAccounts:
                {
                    if (elements == "Graph+PieAccounts" || elements == "PiePortfolio+PieAccounts")
                    {
                        return new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        return new GridLength(0);
                    }
                }
            case FullScreenMode.PieNarratives:
                {
                    if (elements == "PieNarratives")
                    {
                        return new GridLength(1, GridUnitType.Star);
                    }
                    else
                    {
                        return new GridLength(0);
                    }
                }
            default:
                {
                    return gridDef;
                }
        }
    }
}





