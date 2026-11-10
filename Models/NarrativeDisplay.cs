using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CryptoPortfolioTracker.Models;

public partial class NarrativeDisplay : BaseModel
{
    public NarrativeDisplay(string name = "")
    {

    }

    [ObservableProperty] private int id;

    [ObservableProperty] private string name;
    [ObservableProperty] private string about;

    [ObservableProperty] private bool isHoldingCoins;
    [ObservableProperty] private double totalValue;
    [ObservableProperty] private double costBase;
    [ObservableProperty] private double profitLoss;
    [ObservableProperty] private double profitLossPerc;

    [ObservableProperty] private ICollection<CoinDisplay> coins;



}
