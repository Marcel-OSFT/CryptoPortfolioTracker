
namespace CryptoPortfolioTracker.Enums
{
    public enum PriceLevelStatus
    {
        NotWithinRange = 0,
        WithinRange = 1,
        CloseToPrice = 2,
        TaggedPrice = 3,
        TaggedAndCloseToPrice = 4,
        TaggedAndWithinRange = 5,
        TaggedAndNotWithinRange = 6
    }
    public enum PriceLevelType
    {
        TakeProfit = 0,
        Stop = 1,
        Buy = 2,
        Ema = 3,

    }

}
