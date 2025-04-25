using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public class AssetBuilder
{
    private AssetBuilder()
    {
        
    }

    private int _id;
    private double _qty;
    private double _averageCostPrice;
    private double _realizedPnL;

    private Coin _coin = new Coin();
    private Account _account = new Account();
    public ICollection<Mutation> Mutations = new List<Mutation>();

    public static AssetBuilder Create() => new AssetBuilder();

    public AssetBuilder WithId(int id)
    {
        _id = id;
        return this;
    }
    public AssetBuilder QtyOf(double qty)
    {
        _qty = qty;
        return this;
    }
    public AssetBuilder AverageCostPriceOf(double averageCostPrice)
    {
        _averageCostPrice = averageCostPrice;
        return this;
    }

    public AssetBuilder RealizedPnLOf(double realizedPnL)
    {
        _realizedPnL = realizedPnL;
        return this;
    }
    public AssetBuilder WithCoin(Coin coin)
    {
        _coin = coin;
        return this;
    }
    public AssetBuilder WithAccount(Account account)
    {
        _account = account;
        return this;
    }
    public AssetBuilder WithMutations(ICollection<Mutation> mutations)
    {
        Mutations = mutations;
        return this;
    }

    public Asset Build()
    {
        return new Asset
        {
            Qty = _qty,
            AverageCostPrice = _averageCostPrice,
            RealizedPnL = _realizedPnL,
            Coin = _coin,
            Account = _account,
            Mutations = Mutations
        };
    }

}
