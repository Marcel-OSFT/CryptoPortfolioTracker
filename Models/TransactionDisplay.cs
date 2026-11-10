using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class TransactionDisplay : BaseModel
{
    public TransactionDisplay()
    {
        
    }

    // properties obtained from Context
    [ObservableProperty] private int id;
    [ObservableProperty] private DateTime timeStamp;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty] private ICollection<MutationDisplay> mutations;
    [ObservableProperty] private AssetDisplay? requestedAsset = null;
    [ObservableProperty] private TransactionDetails details;

}