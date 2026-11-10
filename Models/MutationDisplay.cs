using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class MutationDisplay : BaseModel
{
    public MutationDisplay()
    {
        
    }

    [ObservableProperty] private int id;
    [ObservableProperty] private TransactionKind type;
    [ObservableProperty] private double qty;
    [ObservableProperty] private double price;
    [ObservableProperty] private MutationDirection direction;

    [ObservableProperty] private AssetDisplay asset;
    [ObservableProperty] private TransactionDisplay transaction;


}
