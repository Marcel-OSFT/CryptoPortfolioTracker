using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models
{

    public partial class AssetAccount : BaseModel
    {
        public AssetAccount()
        {
        }


        [ObservableProperty] string name;

        [ObservableProperty] double qty;

        [ObservableProperty] string symbol;

        [ObservableProperty] int assetId;

    }

}
