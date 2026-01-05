using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;
using System.Reflection;

namespace TemperatureMonitor.Converters
{
    public class DateToStringConverter : IValueConverter
    {
        private readonly Settings _settings;
        // Parameterless ctor so XAML can instantiate the converter.
        public DateToStringConverter() : this(ResolveSettings()) { }

        // Existing ctor for manual DI usage.
        public DateToStringConverter(Settings? settings)
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
                var prop = app.GetType().GetProperty("Container", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
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
            //var format = parameter as string ?? "ddd MMM dd"; // short weekday, short month, day
            var ci = _settings is not null ? new CultureInfo(_settings.AppCultureLanguage) : CultureInfo.CurrentCulture;
            var format = ci.Name == "nl" ? "ddd dd MMM" : "ddd MMM dd";
            
            // accept DateTimeOffset or DateTime
            if (value is DateTimeOffset dto)
            {
                try
                {
                    if (dto.Date == DateTime.Now.Date)
                    {
                        return ci.Name == "nl" ? "Vandaag" : "Today";
                    }
                    if (dto.Date == DateTime.Now.AddDays(-1).Date)
                    {
                        return ci.Name == "nl" ? "Gisteren" : "Yesterday";
                    }

                    return dto.ToString(format, ci);
                }
                catch
                {
                    return dto.ToString(ci);
                }
            }
            else if (value is DateTime dt)
            {
                try
                {
                    if (dt.Date == DateTime.Now.Date)
                    {
                        return ci.Name == "nl" ? "Vandaag" : "Today";
                    }
                    if (dt.Date == DateTime.Now.AddDays(-1).Date)
                    {
                        return ci.Name == "nl" ? "Gisteren" : "Yesterday";
                    }
                    return dt.ToString(format, ci);
                }
                catch
                {
                    return dt.ToString(ci);
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Not needed for one-way display binding
            throw new NotImplementedException();

        }
    }
}