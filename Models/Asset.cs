using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

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
