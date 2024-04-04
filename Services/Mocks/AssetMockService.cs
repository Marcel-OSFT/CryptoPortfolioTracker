using CryptoPortfolioTracker.Models;

namespace CryptoPortfolioTracker.Services
{
    public class AssetMockService
    {
        public AssetMockService()
        {

        }

        public Asset MockAsset1 = new()
        {
            Qty = 0.05,
            AverageCostPrice = 18000,
            Id = 1,

        };
        public Asset MockAsset2 = new()
        {
            Qty = 0.3,
            AverageCostPrice = 200,
            Id = 2,

        };





    }
}
