
namespace CryptoPortfolioTracker.Models;

public class ExchangeRateMissingMessage
{
    public DateOnly Date { get; }
    public string Reason { get; }

    public ExchangeRateMissingMessage(DateOnly date, string reason)
    {
        Date = date;
        Reason = reason;
    }

}

public class DashboardSettingsChangedMessage
{
    public IReadOnlyCollection<string> ChangedSettings { get; }

    public DashboardSettingsChangedMessage(IEnumerable<string> changedSettings)
    {
        ChangedSettings = changedSettings?.ToList() ?? new List<string>();
    }
}
public class UpdateProgressValueMessage
{
    public int ProgressValue { get; }

    public UpdateProgressValueMessage(int value)
    {
        ProgressValue = value;
    }
}

public class UpdatePricesMessage
{
    
}

public class UpdateDashboardMessage
{

}


public class GraphUpdatedMessage
{

}
public class CurrencyChangedMessage
{

}
public class FontSizeChangedMessage
{

}

public class PortfolioConnectionChangedMessage
{

}
public class ShowBePatienceMessage
{

}
