using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CryptoPortfolioTracker.Models;
using System.Collections.Generic;

namespace CryptoPortfolioTracker.Services;

public interface IFunctions
{
    string AddPrefixAndSuffixToString(double value, string prefix, string suffix);
    string AddPrefixAndSuffixToString(string value, string prefix, string suffix);
    GridLength BoolToRowDef(bool value, string trueRowDef, string falseRowDef, XamlRoot root);
    string DateTimeToString(DateTime value);
    SolidColorBrush DoubleToColour(double value, string parameter = "");
    Visibility FalseToVisible(bool value);
    ICollection<Coin> FilterAssetCoins(ICollection<Coin> value);
    int FontSizeToImageSize(double value);
    string FormatDistanceToLevelToString(ICollection<PriceLevel> value1, int index, string format);
    string FormatDoubleAndAddSuffix(double value, string format, string suffix);
    string FormatMaxQtyA(double value);
    string FormatPriceLevelToString(ICollection<PriceLevel> value1, int index, string format);
    Uri FormatUri(string value);
    string FormatValueToString(double value, string format);
    GridLength FullScreenToGridDef(FullScreenMode value, string parameter, string elements);
    string GetAndFormatAssetAverageCost(ICollection<Asset> value, string parameter);
    string Int32ToString(int value, string format);
    bool InvertBool(bool value);
    bool InvertBool(bool? value);
    SolidColorBrush PriceLevelToColor(ICollection<PriceLevel> value1, int index, string parameter);
    SolidColorBrush PriceLevelTypeToColor(ICollection<PriceLevel> value);
    string SetNotAssignedToEmpty(string value);
    ImageSource StringToImageSource(string value);
    ImageSource TransactionDirectionToBitmapImage(MutationDirection value);
    string TransactionKindToString(TransactionKind value);
    Visibility TransactionKindToVisibility(TransactionKind value);
    double TrueToOpacityOne(bool value);
    ScrollMode TrueToScrollModeDisabled(bool value);
    Visibility TrueToVisible(bool value);
    Visibility TrueToVisible(bool? value);
    SolidColorBrush ValueToRedOrGreenBackground(double value);
    Visibility ZeroToCollapsed(double value);
}