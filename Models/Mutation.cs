using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class Mutation : BaseModel
{
    public Mutation()
    {
        Asset = new Asset();
        Transaction = new Transaction();
    }

    //******* Public Properties
    public int Id { get; set; }
    public TransactionKind Type { get; set; }
    public double Qty { get; set; }
    public double Price { get; set; }
    public MutationDirection Direction { get; set; }

    //******* Navigation Properties
    public Asset Asset { get; set; }
    public Transaction Transaction { get; set; }


}
