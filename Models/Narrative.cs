using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptoPortfolioTracker.Models;

public partial class Narrative : BaseModel
{
    public Narrative(string name = "")
    {
        Name = name;
        About = string.Empty;
        Coins = new List<Coin>();
    }

    //******* Public Properties
    [Key]
    public int Id { get; set; }

    public string Name { get; set; }
    public string About { get; set; }

    //*** Set To NotMapped in EntityTypeBuilder
    [NotMapped] public bool IsHoldingCoins { get; set; }
    //*** Set To NotMapped in EntityTypeBuilder
    [ObservableProperty] private double totalValue;
    //*** Set To NotMapped in EntityTypeBuilder
    [ObservableProperty] private double costBase;
    //*** Set To NotMapped in EntityTypeBuilder
    public double ProfitLoss { get; set; }
    //*** Set To NotMapped in EntityTypeBuilder
    public double ProfitLossPerc { get; set; }

    //*** Navigation property
    public ICollection<Coin> Coins { get; set; }


    partial void OnCostBaseChanged(double value)
    {
        ProfitLoss = TotalValue - value;
        ProfitLossPerc = 100 * ((TotalValue - value) / value);
    }
    partial void OnTotalValueChanged(double value)
    {
        ProfitLoss = value - CostBase;
        ProfitLossPerc = 100 * ((value - CostBase) / CostBase);
    }

}
