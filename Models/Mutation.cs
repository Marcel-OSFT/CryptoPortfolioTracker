using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using System;


namespace CryptoPortfolioTracker.Models;

public partial class Mutation : BaseModel
{
    public Mutation()
    {
        Asset = new Asset();
        Transaction = new Transaction();
    }

    //******* Public Properties

    [ObservableProperty] private int id;
    [ObservableProperty] private TransactionKind type;
    [ObservableProperty] private double qty;
    [ObservableProperty] private double price;
    [ObservableProperty] private MutationDirection direction;

    //******* Navigation Properties

    [ObservableProperty] private Asset asset;
    [ObservableProperty] private Transaction transaction;


}
