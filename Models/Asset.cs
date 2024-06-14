using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoPortfolioTracker.Models;

public partial class Asset : BaseModel
{
    public Asset()
    {
        Coin = new Coin();
        Account = new Account();
        Mutations = new List<Mutation>();
    }

    [ObservableProperty] private int id;
    [ObservableProperty] private double qty;
    [ObservableProperty] private double averageCostPrice;
    [ObservableProperty] private double realizedPnL;

    //******* Navigation Properties
    [ObservableProperty] private Coin coin;
    [ObservableProperty] private Account account;
    public ICollection<Mutation> Mutations { get; set; }

    
}
