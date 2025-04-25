using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class Transaction : BaseModel
{
    public Transaction()
    {
        Mutations = new Collection<Mutation>();
        Details = new TransactionDetails
        {
            FeeCoinSymbol = "ETH",
            FeeCoinName = "Ethereum"
        };
    }

    #region Properties
    // properties obtained from Context
    [ObservableProperty] private int id;
    [ObservableProperty] private DateTime timeStamp;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty] private ICollection<Mutation> mutations;
    [ObservableProperty][NotMapped] private Asset? requestedAsset = null;
    [ObservableProperty][NotMapped] private TransactionDetails details;

    partial void OnMutationsChanged(ICollection<Mutation> value)
    {
        if (value != null && value.Count > 0) ExtractDetailsFromMutations();
    }

    #endregion Properties

    public void ExtractDetailsFromMutations()
    {
        if (Mutations == null || Mutations.Count == 0)
        {
            return;
        }
        try
        {
            var transactionType = Mutations.ElementAt(0).Type;

            //Details.TransactionType = Mutations.ElementAt(0).Type;
            var muts = new List<Mutation>(Mutations
                .OrderByDescending(d => d.Direction)
                .OrderBy(t => t.Type));
            foreach (var mutation in muts)
            {
                switch (mutation.Type)
                {
                    case TransactionKind.Deposit:
                        {
                            Details = new TransactionDetailsBuilder(Details)
                                .OfTransactionType(transactionType)
                                .FromCoinSymbol(mutation.Asset.Coin.Symbol)
                                .FromCoinName(mutation.Asset.Coin.Name)
                                .FromImage(mutation.Asset.Coin.ImageUri)
                                .FromQty(mutation.Qty)
                                .FromPrice(mutation.Price)
                                .FromValue(mutation.Qty * mutation.Price)
                                .FromAccount(mutation.Asset.Account.Name)
                                .Direction(MutationDirection.In)
                                .Build();
                            break;
                        }
                    case TransactionKind.Withdraw:
                        {
                            Details = new TransactionDetailsBuilder(Details)
                                .OfTransactionType(transactionType)
                                .FromCoinSymbol(mutation.Asset.Coin.Symbol)
                                .FromCoinName(mutation.Asset.Coin.Name)
                                .FromImage(mutation.Asset.Coin.ImageUri)
                                .FromQty(mutation.Qty)
                                .FromPrice(mutation.Price)
                                .FromValue(mutation.Qty * mutation.Price)
                                .FromAccount(mutation.Asset.Account.Name)
                                .Direction(MutationDirection.Out)
                                .Build();
                            break;
                        }
                    case TransactionKind.Transfer:
                        {
                            if (transactionType == TransactionKind.Transfer) // solely a transfer transaction
                            {
                                if (mutation.Direction == MutationDirection.Out)
                                {
                                    Details = new TransactionDetailsBuilder(Details)
                                        .OfTransactionType(transactionType)
                                        .FromCoinSymbol(mutation.Asset.Coin.Symbol)
                                        .FromCoinName(mutation.Asset.Coin.Name)
                                        .FromImage(mutation.Asset.Coin.ImageUri)
                                        .FromQty(mutation.Qty)
                                        .FromPrice(mutation.Price)
                                        .FromValue(mutation.Qty * mutation.Price)
                                        .FromAccount(mutation.Asset.Account.Name)
                                        .Direction(RequestedAsset != null && RequestedAsset.Account.Name == mutation.Asset.Account.Name ? MutationDirection.Out : MutationDirection.In)
                                        .Build();
                                }
                                else //IN
                                {
                                    //Everything is already set except the ToAccount
                                    Details = new TransactionDetailsBuilder(Details)
                                        .ToAccount(mutation.Asset.Account.Name)
                                        .Build();
                                }
                            }
                            else if (mutation.Direction == MutationDirection.In) // transaction (sell, buy, convert) combined with transfer 
                            {
                                //both 'passes' (IN and OUT) have same paramaters, so to prevent a second pass only do this on the OUT
                                Details = new TransactionDetailsBuilder(Details)
                                        .OfTransactionType(transactionType)
                                        .ToCoinSymbol(mutation.Asset.Coin.Symbol)
                                        .ToCoinName(mutation.Asset.Coin.Name)
                                        .ToImage(mutation.Asset.Coin.ImageUri)
                                        .ToQty(mutation.Qty)
                                        .ToPrice(mutation.Price)
                                        .ToValue(mutation.Qty * mutation.Price)
                                        .ToAccount(mutation.Asset.Account.Name)
                                        .Build();
                            }
                            break;
                        }
                    case TransactionKind.Convert:
                    case TransactionKind.Buy:
                    case TransactionKind.Sell:
                        {
                            if (mutation.Direction == MutationDirection.In)
                            {
                                Details = new TransactionDetailsBuilder(Details)
                                        .OfTransactionType(transactionType)
                                        .ToCoinSymbol(mutation.Asset.Coin.Symbol)
                                        .ToCoinName(mutation.Asset.Coin.Name)
                                        .ToImage(mutation.Asset.Coin.ImageUri)
                                        .ToQty(mutation.Qty)
                                        .ToPrice(mutation.Price)
                                        .ToValue(mutation.Qty * mutation.Price)
                                        .ToAccount(mutation.Asset.Account.Name)
                                        .Direction((RequestedAsset != null && (RequestedAsset.Coin.Symbol == mutation.Asset.Coin.Symbol && RequestedAsset.Coin.Name == mutation.Asset.Coin.Name)) 
                                            ? MutationDirection.In 
                                            : MutationDirection.Out)
                                        .Build();
                            }
                            else
                            {
                                Details = new TransactionDetailsBuilder(Details)
                                        .OfTransactionType(transactionType)
                                        .FromCoinSymbol(mutation.Asset.Coin.Symbol)
                                        .FromCoinName(mutation.Asset.Coin.Name)
                                        .FromImage(mutation.Asset.Coin.ImageUri)
                                        .FromQty(mutation.Qty)
                                        .FromPrice(mutation.Price)
                                        .FromValue(mutation.Qty * mutation.Price)
                                        .FromAccount(mutation.Asset.Account.Name)
                                        .Build();
                            }
                            break;
                        }
                    case TransactionKind.Fee:
                        {
                            Details = new TransactionDetailsBuilder(Details)
                                        .OfTransactionType(transactionType)
                                        .FeeCoinSymbol(mutation.Asset.Coin.Symbol)
                                        .FeeCoinName(mutation.Asset.Coin.Name)
                                        .FeeImage(mutation.Asset.Coin.ImageUri)
                                        .FeeQty(mutation.Qty)
                                        .Build();
                            break;
                        }
                    default: break;
                }
            }
        }
        catch (Exception ) {  }
    }

}


