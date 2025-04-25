using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using System;

namespace CryptoPortfolioTracker.Models;

public class MutationBuilder
{
    public MutationBuilder()
    {

    }

    private int _id;
    private TransactionKind _type;
    private double _qty;
    private double _price;
    private MutationDirection _direction;

    private Asset _asset = new Asset();
    private Transaction _transaction = new Transaction();

    public static MutationBuilder Create() => new MutationBuilder();

    
    public MutationBuilder OfType(TransactionKind type)
    {
        _type = type;
        return this;
    }
    public MutationBuilder QtyOf(double qty)
    {
        _qty = qty;
        return this;
    }
    public MutationBuilder PriceOf(double price)
    {
        _price = price;
        return this;
    }
    public MutationBuilder Direction(MutationDirection direction)
    {
        _direction = direction;
        return this;
    }
    public MutationBuilder WithAsset(Action<AssetBuilder> options)
    {
        var asset = AssetBuilder.Create();
        options(asset);
        _asset = asset.Build();
        return this;
    }
    public MutationBuilder OfTransaction(Transaction transaction)
    {
        _transaction = transaction;
        return this;
    }
    public Mutation Build()
    {
        return new Mutation
        {
            Id = _id,
            Type = _type,
            Qty = _qty,
            Price = _price,
            Direction = _direction,
            Asset = _asset,
            Transaction = _transaction
        };
    }
}
