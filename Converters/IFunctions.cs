using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using TemperatureMonitor.Models;
using System.Collections.Generic;

namespace TemperatureMonitor.Services;

public interface IFunctions
{
    string AddPrefixAndSuffixToString(double value, string prefix, string suffix);
    string AddPrefixAndSuffixToString(string value, string prefix, string suffix);
    GridLength BoolToRowDef(bool value, string trueRowDef, string falseRowDef, XamlRoot root);
    string DateTimeToString(DateTime value);
    SolidColorBrush DoubleToColour(double value, string parameter = "");
    Visibility FalseToVisible(bool value);
    int FontSizeToImageSize(double value);
    string FormatDoubleAndAddSuffix(double value, string format, string suffix);
    string FormatMaxQtyA(double value);
    Uri FormatUri(string value);
    string FormatValueToString(double value, string format);
    string Int32ToString(int value, string format);
    bool InvertBool(bool value);
    bool InvertBool(bool? value);
    string SetNotAssignedToEmpty(string value);
    ImageSource StringToImageSource(string value);
    double TrueToOpacityOne(bool value);
    ScrollMode TrueToScrollModeDisabled(bool value);
    Visibility TrueToVisible(bool value);
    Visibility TrueToVisible(bool? value);
    SolidColorBrush ValueToRedOrGreenBackground(double value);
    Visibility ZeroToCollapsed(double value);
}