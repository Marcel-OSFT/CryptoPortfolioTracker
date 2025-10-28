using CryptoPortfolioTracker.Services;
using Microsoft.UI.Xaml.Data;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;


namespace CryptoPortfolioTracker.Converters;

public class FormatValueToString : IValueConverter
{
    private readonly Settings _settings;
    // Parameterless ctor so XAML can instantiate the converter.
    public FormatValueToString() : this(ResolveSettings()) { }

    // Existing ctor for manual DI usage.
    public FormatValueToString(Settings? settings)
    {
        _settings = settings;
    }

    private static Settings? ResolveSettings()
    {
        try
        {
            var app = Microsoft.UI.Xaml.Application.Current;
            if (app == null)
                return null;

            // If your App exposes a ServiceProvider property (common pattern), try to resolve Settings:
            var prop = app.GetType().GetProperty("Services", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var services = prop?.GetValue(app) as IServiceProvider;
            if (services != null)
                return services.GetService(typeof(Settings)) as Settings;
        }
        catch
        {
            // swallow: fallback to null and use culture defaults
        }
        return null;
    }
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (parameter == null)
        {
            return value;
        }

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

        if (value is double)
        {
            return string.Format(ci, (string)parameter, (double)value);
        }
        else if (value is int)
        {
            return string.Format(ci, (string)parameter, (int)value);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        double result;
        try
        {
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

            if (targetType == typeof(double))
            {
                return System.Convert.ToDouble((string)value, ci);
            }
            else if (targetType == typeof(int))
            {
                return System.Convert.ToInt16((string)value, ci);
            }

        }
        catch
        {
            result = 0;
        }
        return 0;
    }

}