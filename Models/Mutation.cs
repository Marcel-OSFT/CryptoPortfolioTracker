using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;


namespace CryptoPortfolioTracker.Models
{
    public partial class Mutation : BaseModel
    {
        public Mutation()
        {
            Asset = new Asset();
            Transaction = new Transaction();
        }

        //******* Public Properties

        [ObservableProperty] int id;
        [ObservableProperty] TransactionKind type;
        [ObservableProperty] double qty;
        [ObservableProperty] double price;
        [ObservableProperty] MutationDirection direction;

        //******* Navigation Properties

        [ObservableProperty] Asset asset;
        [ObservableProperty] Transaction transaction;


    }
}
