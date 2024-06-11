using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Helpers;


namespace CryptoPortfolioTracker.Models;

public partial class Asset : BaseModel
{
    //***** Constructor
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
