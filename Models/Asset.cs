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

    public int Id { get; set; }
    public double Qty { get; set; }
    public double AverageCostPrice { get; set; }
    public double RealizedPnL { get; set; }

    //******* Navigation Properties
    public Coin Coin { get; set; }
    public Account Account { get; set; }
    public ICollection<Mutation> Mutations { get; set; }

    
}
