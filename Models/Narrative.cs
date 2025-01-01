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

    [ObservableProperty] private string name;
    [ObservableProperty] private string about;

    //*** Navigation property
    [ObservableProperty] private ICollection<Coin> coins;

    //*** Set To NotMapped in EntityTypeBuilder
    [ObservableProperty] [NotMapped] private bool isHoldingCoins;
    //*** Set To NotMapped in EntityTypeBuilder
    [ObservableProperty] private double totalValue;

    



}
