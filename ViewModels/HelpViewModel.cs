
//using CoinGecko.ApiEndPoints;
//using CoinGecko.Clients;
using CommunityToolkit.Mvvm.ComponentModel;
namespace CryptoPortfolioTracker.ViewModels
{
    public sealed partial class HelpViewModel : BaseViewModel
    {

        [ObservableProperty] private string helpText;

        public static HelpViewModel Current;

        public HelpViewModel()
        {
            Current = this;
        }


    }

}

