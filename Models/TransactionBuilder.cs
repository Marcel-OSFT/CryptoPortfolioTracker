using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public class TransactionBuilder
{
    private TransactionBuilder()
    {
        //Mutations = new Collection<Mutation>();
        //Details = new TransactionDetails
        //{
        //    FeeCoinSymbol = "ETH",
        //    FeeCoinName = "Ethereum"
        //};
    }
    
    private int _id;
    private DateTime _timeStamp;
    private string _note = string.Empty;
    private ICollection<Mutation> _mutations;
    private Asset? _requestedAsset = null;
    private TransactionDetails _details;

    public static TransactionBuilder Create() => new TransactionBuilder();

    public TransactionBuilder WithId(int id)
    {
        _id = id;
        return this;
    }
    public TransactionBuilder ExecutedOn(DateTime timeStamp)
    {
        _timeStamp = timeStamp;
        return this;
    }
    public TransactionBuilder WithNote(string note)
    {
        _note = note;
        return this;
    }
    public TransactionBuilder WithMutation(Action<MutationBuilder> options)
    {
        var mutation = MutationBuilder.Create();
        options(mutation);
        _mutations.Add(mutation.Build());
        return this;
    }
    public TransactionBuilder WithMutations(ICollection<Mutation> mutations)
    {
        _mutations = mutations;
        return this;
    }
    public TransactionBuilder ForAsset(Asset requestedAsset)
    {
        _requestedAsset = requestedAsset;
        return this;
    }
    public TransactionBuilder WithDetails(Action<TransactionDetailsBuilder> options)
    {
        var details = TransactionDetailsBuilder.Create();
        options(details);
        _details = details.Build();
        return this;
    }
    public Transaction Build()
    {
        return new Transaction
        {
            Id = _id,
            TimeStamp = _timeStamp,
            Note = _note,
            Mutations = _mutations,
            RequestedAsset = _requestedAsset,
            Details = _details
        };
    }


}


