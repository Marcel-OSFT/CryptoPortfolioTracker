using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;

namespace CryptoPortfolioTracker.Models;

public partial class Transaction : BaseModel, IDisposable
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
            Details.TransactionType = Mutations.ElementAt(0).Type;
            var muts = new List<Mutation>(Mutations
                .OrderByDescending(d => d.Direction)
                .OrderBy(t => t.Type));
            foreach (var mutation in muts)
            {
                switch (mutation.Type)
                {
                    case TransactionKind.Deposit:
                        {
                            Details.CoinASymbol = mutation.Asset.Coin.Symbol;
                            Details.CoinAName = mutation.Asset.Coin.Name;
                            Details.ImageUriA = mutation.Asset.Coin.ImageUri;
                            Details.QtyA = mutation.Qty;
                            Details.PriceA = mutation.Price;
                            Details.ValueA = Details.QtyA * Details.PriceA;
                            Details.AccountFrom = mutation.Asset.Account.Name;
                            Details.TransactionDirection = MutationDirection.In;
                            break;
                        }
                    case TransactionKind.Withdraw:
                        {
                            Details.CoinASymbol = mutation.Asset.Coin.Symbol;
                            Details.CoinAName = mutation.Asset.Coin.Name;
                            Details.ImageUriA = mutation.Asset.Coin.ImageUri;
                            Details.QtyA = mutation.Qty;
                            Details.PriceA = mutation.Price;
                            Details.ValueA = Details.QtyA * Details.PriceA;
                            Details.AccountFrom = mutation.Asset.Account.Name;
                            Details.TransactionDirection = MutationDirection.Out;
                            break;
                        }
                    case TransactionKind.Transfer:
                        {
                            if (Details.TransactionType == TransactionKind.Transfer) // solely a transfer transaction
                            {
                                if (mutation.Direction == MutationDirection.Out)
                                {
                                    Details.CoinASymbol = mutation.Asset.Coin.Symbol;
                                    Details.CoinAName = mutation.Asset.Coin.Name;
                                    Details.ImageUriA = mutation.Asset.Coin.ImageUri;
                                    Details.QtyA = mutation.Qty;
                                    Details.PriceA = mutation.Price;
                                    Details.ValueA = Details.QtyA * Details.PriceA;
                                    Details.AccountFrom = mutation.Asset.Account.Name;
                                }
                                else
                                {
                                    Details.AccountTo = mutation.Asset.Account.Name;
                                }
                                //*** set transactionDirection voor Icon in View
                                Details.AccountTo = mutation.Asset.Account.Name;
                                if (RequestedAsset != null && RequestedAsset.Account.Name == Details.AccountTo)
                                {
                                    Details.TransactionDirection = MutationDirection.In;
                                }
                                else
                                {
                                    Details.TransactionDirection = MutationDirection.Out;
                                }
                            }
                            else // transaction combined with transfer 
                            {
                                Details.CoinBSymbol = mutation.Asset.Coin.Symbol;
                                Details.CoinBName = mutation.Asset.Coin.Name;
                                Details.ImageUriB = mutation.Asset.Coin.ImageUri;
                                Details.QtyB = mutation.Qty;
                                Details.PriceB = mutation.Price;
                                Details.ValueB = Details.QtyB * Details.PriceB;
                                Details.AccountTo = mutation.Asset.Account.Name;
                            }
                            break;
                        }
                    case TransactionKind.Convert:
                    case TransactionKind.Buy:
                    case TransactionKind.Sell:
                        {
                            if (mutation.Direction == MutationDirection.In)
                            {
                                Details.CoinBSymbol = mutation.Asset.Coin.Symbol;
                                Details.CoinBName = mutation.Asset.Coin.Name;
                                Details.ImageUriB = mutation.Asset.Coin.ImageUri;
                                Details.QtyB = mutation.Qty;
                                Details.PriceB = mutation.Price;
                                Details.ValueB = Details.QtyB * Details.PriceB;
                                Details.AccountTo = mutation.Asset.Account.Name;

                                if (RequestedAsset != null && (RequestedAsset.Coin.Symbol == Details.CoinBSymbol && RequestedAsset.Coin.Name == Details.CoinBName))
                                {
                                    Details.TransactionDirection = MutationDirection.In;
                                }
                                else
                                {
                                    Details.TransactionDirection = MutationDirection.Out;
                                }
                            }
                            else
                            {
                                Details.CoinASymbol = mutation.Asset.Coin.Symbol;
                                Details.CoinAName = mutation.Asset.Coin.Name;
                                Details.ImageUriA = mutation.Asset.Coin.ImageUri;
                                Details.QtyA = mutation.Qty;
                                Details.PriceA = mutation.Price;
                                Details.ValueA = Details.QtyA * Details.PriceA;
                                Details.AccountFrom = mutation.Asset.Account.Name;
                            }
                            break;
                        }
                    case TransactionKind.Fee:
                        {
                            Details.FeeCoinSymbol = mutation.Asset.Coin.Symbol;
                            Details.FeeCoinName = mutation.Asset.Coin.Name;
                            Details.ImageUriFee = mutation.Asset.Coin.ImageUri;
                            Details.FeeQty = mutation.Qty;
                            break;
                        }
                    default: break;
                }
            }
        }
        catch (Exception ) {  }
    }

    public void Dispose()
    {
        mutations.Clear();
        mutations = null;

    }
}


