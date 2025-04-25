
namespace CryptoPortfolioTracker.Models;

public class UpdatePricesMessage
{
    public Coin Coin { get; }
    public UpdatePricesMessage(Coin coin)
    {
        Coin = coin;
    }
}

public class UpdateDashboardMessage
{

}

public class UpdateProgressValueMessage
{
    public int ProgressValue { get; }

    public UpdateProgressValueMessage(int value)
    {
        ProgressValue = value;
    }
}

public class GraphUpdatedMessage
{
    
}

public class PortfolioConnectionChangedMessage
{

}
public class ShowBePatienceMessage
{

}

public class PreferencesChangedMessage
{

}