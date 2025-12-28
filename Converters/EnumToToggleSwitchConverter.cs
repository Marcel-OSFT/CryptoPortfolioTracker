using System;
using Microsoft.UI.Xaml.Data;

namespace TemperatureMonitor.Converters
{
    // Maps an enum with two values to ToggleSwitch.IsOn.
    // ConverterParameter should be the enum name that represents the "On" (right) state, e.g. "Dag".
    // Convert: enum -> bool (true when enum == parameter)
    // ConvertBack: true -> parameter enum value; false -> the other enum value (assumes a 2-value enum)
    public class EnumToToggleSwitchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is null || value is null) return false;
            var paramName = parameter.ToString();
            return string.Equals(value.ToString(), paramName, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var isOn = value is bool b && b;
            if (parameter is null) return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;

            // If binding targetType is nullable enum, get underlying enum type
            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (!enumType.IsEnum) return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;

            var onName = parameter.ToString();

            if (isOn)
            {
                try { return Enum.Parse(enumType, onName, ignoreCase: true); }
                catch { return Microsoft.UI.Xaml.DependencyProperty.UnsetValue; }
            }

            // isOff: return first enum value that's not the "on" value (assumes two-state enum)
            foreach (var name in Enum.GetNames(enumType))
            {
                if (!string.Equals(name, onName, StringComparison.OrdinalIgnoreCase))
                {
                    return Enum.Parse(enumType, name);
                }
            }

            return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
        }
    }
}