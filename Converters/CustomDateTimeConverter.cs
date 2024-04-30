using Newtonsoft.Json.Converters;

namespace CryptoPortfolioTracker.Converters;

public class CustomDateTimeConverter : IsoDateTimeConverter
{
    public CustomDateTimeConverter(string format)
    {
        DateTimeFormat = format;
    }
}