//using ABI.Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;


namespace CryptoPortfolioTracker.Models
{
    public partial class Asset : BaseModel
    {
        //***** Constructor
        public Asset()
        {
            Coin = new Coin();
            Account = new Account();
        }

        [ObservableProperty] int id;
        [ObservableProperty] double qty;
        [ObservableProperty] double averageCostPrice;
        [ObservableProperty] double realizedPnL;

        //******* Navigation Properties
        [ObservableProperty] Coin coin;
        [ObservableProperty] Account account;
        public ICollection<Mutation> Mutations { get; set; }


    }
}
