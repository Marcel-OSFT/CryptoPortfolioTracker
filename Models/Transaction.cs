using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Enums;
using LanguageExt.Common;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CryptoPortfolioTracker.Models
{

    public partial class Transaction : BaseModel
    {
        
        public Transaction()
        {
            Mutations = new Collection<Mutation>();
            Details = new TransactionDetails();
            Details.FeeCoin = "ETH";
        }

        #region Properties
        // properties obtained from Context
        [ObservableProperty] int id;
        [ObservableProperty] DateTime timeStamp;
        [ObservableProperty] string note = string.Empty;

        [ObservableProperty] ICollection<Mutation> mutations;

        [ObservableProperty] [NotMapped] Asset requestedAsset = null;
        [ObservableProperty] [NotMapped] TransactionDetails details;


        partial void OnMutationsChanged(ICollection<Mutation> value)
        {
            if (value != null && value.Count > 0) ExtractDetailsFromMutations();
        }

        #endregion Properties

        public void ExtractDetailsFromMutations()
        {
            if (Mutations == null || Mutations.Count == 0) return;
            try
            {
                Details.TransactionType = Mutations.ElementAt(0).Type;
                List<Mutation> muts = new List<Mutation>(Mutations.OrderByDescending(d => d.Direction).OrderBy(t => t.Type));
                foreach (var mutation in muts)
                {
                    switch (mutation.Type)
                    {
                        case TransactionKind.Deposit:
                            {
                                Details.CoinA = mutation.Asset.Coin.Symbol;
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
                                Details.CoinA = mutation.Asset.Coin.Symbol;
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
                                        Details.CoinA = mutation.Asset.Coin.Symbol;
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
                                    else Details.TransactionDirection = MutationDirection.Out;
                                }
                                else // transaction combined with transfer 
                                {
                                    Details.CoinB = mutation.Asset.Coin.Symbol;
                                    Details.ImageUriB = mutation.Asset.Coin.ImageUri;
                                    Details.QtyB = mutation.Qty;
                                    Details.PriceB = mutation.Price;
                                    Details.ValueB = Details.QtyA * Details.PriceA;
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
                                    Details.CoinB = mutation.Asset.Coin.Symbol;
                                    Details.ImageUriB = mutation.Asset.Coin.ImageUri;
                                    Details.QtyB = mutation.Qty;
                                    Details.PriceB = mutation.Price;
                                    Details.ValueB = Details.QtyB * Details.PriceB;
                                    Details.AccountTo = mutation.Asset.Account.Name;

                                    if (RequestedAsset != null && RequestedAsset.Coin.Symbol == Details.CoinB)
                                    {
                                        Details.TransactionDirection = MutationDirection.In;
                                    }
                                    else Details.TransactionDirection = MutationDirection.Out;
                                }
                                else
                                {
                                    Details.CoinA = mutation.Asset.Coin.Symbol;
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
                                Details.FeeCoin = mutation.Asset.Coin.Symbol;
                                Details.ImageUriFee = mutation.Asset.Coin.ImageUri;
                                Details.FeeQty = mutation.Qty;
                                break;
                            }
                        default: break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
            }
        }

        


    }

}


