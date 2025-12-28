using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace TemperatureMonitor.Converters
{
    // Supports two uses:
    // - Convert enum value -> bool (IsChecked binding)
    // - Convert enum value -> Visibility when ConverterTargetVisibility=true (used by NavRow)
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        // special attached-like usage: pass ConverterParameterIsEnum=true and ConverterTargetVisibility=true to get Visibility result
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string param)
            {
                if (param.StartsWith("Visibility:", StringComparison.OrdinalIgnoreCase))
                {
                    // format: "Visibility:EnumName" => returns Visibility.Visible when enum matches
                    var enumName = param.Substring("Visibility:".Length);
                    bool matches = value?.ToString() == enumName;
                    return matches ? Visibility.Visible : Visibility.Collapsed;
                }

                // plain enum name
                bool matches2 = value?.ToString() == param;
                if (targetType == typeof(bool) || targetType == typeof(bool?))
                    return matches2;
                if (targetType == typeof(Visibility))
                    return matches2 ? Visibility.Visible : Visibility.Collapsed;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (!(parameter is string param)) return DependencyProperty.UnsetValue;
            // support nullable enum target types
            var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (!enumType.IsEnum) return DependencyProperty.UnsetValue;

            bool use = false;
            if (value is bool b) use = b;
            if (!use) return DependencyProperty.UnsetValue;

            try
            {
                // ignore-case parse for resiliency
                return Enum.Parse(enumType, param, ignoreCase: true);
            }
            catch
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }

    // Simpler Bool->Visibility for direct binding if needed
    public sealed class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }
}