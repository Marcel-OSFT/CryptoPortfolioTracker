using CryptoPortfolioTracker.Enums;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CryptoPortfolioTracker.Models;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Automation.Peers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace CryptoPortfolioTracker.Models
{

    public class AssetTransaction : BaseModel
    {
        #region Fields
        private TransactionKind transactionType;
        private MutationDirection transactionDirection;
        private string imageUriA;
        private string imageUriB;
        private string imageUriFee;
        private string coinA;
        private string coinB;
        private string accountFrom;
        private string accountTo;
        private double qtyA;
        private double qtyB;
        private double priceA;
        private double priceB;
        private double valueA;
        private double valueB;
        private int id;
        private string feeCoin;
        private double feeQty;
        private string note;
        private Asset requestedAsset;
        private DateTime timeStamp;
        private ICollection<Mutation> mutations;

        public static StandardUICommand commandDel = new StandardUICommand();
        public static XamlUICommand commandEdit = new XamlUICommand();
        

        #endregion Fields
        public AssetTransaction()
        {
            CommandDel = InitStandardUICommand(commandDel, StandardUICommandKind.Delete, "Delete Transaction", "Delete");
            CommandEdit = InitXamlUICommand(commandEdit, "\uE70F", FontWeights.Normal, "Edit Transaction", "Edit");
            Mutations = new Collection<Mutation>();
            FeeCoin = "ETH";
        }

        #region Properties
        // properties obtained from Context   

        [NotMapped] public ICommand CommandEdit { get; set; }
        [NotMapped] public ICommand CommandDel { get; set; }
        
        public int Id
        {
            get { return id; }
            set
            {
                if (value != id)
                {
                    id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }


        [NotMapped]
        public Asset RequestedAsset
        {
            get { return requestedAsset; }
            set
            {
                if (value != requestedAsset)
                {
                    requestedAsset = value;
                    OnPropertyChanged(nameof(RequestedAsset));
                }
            }
        }
        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set
            {
                if (value != timeStamp)
                {
                    timeStamp = value;
                    OnPropertyChanged(nameof(TimeStamp));
                }
            }
        }
        public string Note
        {
            get { return note; }
            set
            {
                if (value != note)
                {
                    note = value;
                    OnPropertyChanged(nameof(Note));
                }
            }
        }
        public ICollection<Mutation> Mutations
        {
            get { return mutations; }
            set
            {
                if (value != mutations)
                {
                    mutations = value;
                    if (mutations!=null && mutations.Count>0) ExtractBindingProperties();
                    OnPropertyChanged(nameof(Mutations));
                }
            }
        }

        // Binding Properties extracted from obtained Context Properties
        // to provide binding with the View

        [NotMapped]
        public TransactionKind TransactionType
        {
            get { return transactionType; }
            set
            {
                if (value != transactionType)
                {
                    transactionType = value;
                    OnPropertyChanged(nameof(TransactionType));
                }
            }
        }
        [NotMapped]
        public string ImageUriFee
        {
            get { return imageUriFee; }
            set
            {
                if (value != imageUriFee)
                {
                    imageUriFee = value;
                    OnPropertyChanged(nameof(ImageUriFee));
                }
            }
        }
        [NotMapped]
        public MutationDirection TransactionDirection
        {
            get { return transactionDirection; }
            set
            {
                if (value != transactionDirection)
                {
                    transactionDirection = value;
                    OnPropertyChanged(nameof(TransactionDirection));
                }
            }
        }
        [NotMapped]
        public string ImageUriA
        {
            get { return imageUriA; }
            set
            {
                if (value != imageUriA)
                {
                    imageUriA = value;
                    OnPropertyChanged(nameof(ImageUriA));
                }
            }
        }
        [NotMapped]
        public string ImageUriB
        {
            get { return imageUriB; }
            set
            {
                if (value != imageUriB)
                {
                    imageUriB = value;
                    OnPropertyChanged(nameof(ImageUriB));
                }
            }
        }
        [NotMapped]
        public string CoinA
        {
            get { return coinA; }
            set
            {
                if (value != coinA)
                {
                    coinA = value;
                    OnPropertyChanged(nameof(CoinA));
                }
            }
        }
        [NotMapped]
        public string CoinB
        {
            get { return coinB; }
            set
            {
                if (value != coinB)
                {
                    coinB = value;
                    OnPropertyChanged(nameof(CoinB));
                }
            }
        }
        [NotMapped]
        public string FeeCoin
        {
            get { return feeCoin; }
            set
            {
                if (value != feeCoin)
                {
                    feeCoin = value;
                    OnPropertyChanged(nameof(FeeCoin));
                }
            }
        }
        [NotMapped]
        public string AccountFrom
        {
            get { return accountFrom; }
            set
            {
                if (value != accountFrom)
                {
                    accountFrom = value;
                    OnPropertyChanged(nameof(AccountFrom));
                }
            }
        }
        [NotMapped]
        public string AccountTo
        {
            get { return accountTo; }
            set
            {
                if (value != accountTo)
                {
                    accountTo = value;
                    OnPropertyChanged(nameof(AccountTo));
                }
            }
        }


        [NotMapped]
        public double QtyA
        {
            get { return qtyA; }
            set
            {
                if (value != qtyA)
                {
                    qtyA = value;
                    OnPropertyChanged(nameof(QtyA));
                }
            }
        }
        [NotMapped]
        public double QtyB
        {
            get { return qtyB; }
            set
            {
                if (value != qtyB)
                {
                    qtyB = value;
                    OnPropertyChanged(nameof(QtyB));
                }
            }
        }

        [NotMapped]
        public double PriceA
        {
            get { return priceA; }
            set
            {
                if (value != priceA)
                {
                    priceA = value;
                    OnPropertyChanged(nameof(PriceA));
                }
            }
        }
        [NotMapped]
        public double PriceB
        {
            get { return priceB; }
            set
            {
                if (value != priceB)
                {
                    priceB = value;
                    OnPropertyChanged(nameof(PriceB));
                }
            }
        }
        [NotMapped]
        public double ValueA
        {
            get { return valueA; }
            set
            {
                if (value != valueA)
                {
                    valueA = value;
                    OnPropertyChanged(nameof(ValueA));
                }
            }
        }
        [NotMapped]
        public double ValueB
        {
            get { return valueB; }
            set
            {
                if (value != valueB)
                {
                    valueB = value;
                    OnPropertyChanged(nameof(ValueB));
                }
            }
        }
        [NotMapped]
        public double FeeQty
        {
            get { return feeQty; }
            set
            {
                if (value != feeQty)
                {
                    feeQty = value;
                   
                    OnPropertyChanged(nameof(FeeQty));
                }
            }
        }
        #endregion Properties

        #region Methods

        public void ExtractBindingProperties()
        {
            try
            {
                transactionType = mutations.ElementAt(0).Type;
                List<Mutation> muts = new List<Mutation>(mutations.OrderByDescending(d => d.Direction).OrderBy(t => t.Type));
                foreach (var mutation in muts)
                {
                    switch (mutation.Type)
                    {
                        case TransactionKind.Deposit:
                            {
                                CoinA = mutation.Asset.Coin.Symbol;
                                ImageUriA = mutation.Asset.Coin.ImageUri;
                                QtyA = mutation.Qty;
                                PriceA = mutation.Price;
                                ValueA = qtyA * priceA;
                                AccountFrom = mutation.Asset.Account.Name;
                                TransactionDirection = MutationDirection.In;

                                break;
                            }
                        case TransactionKind.Withdraw:
                            {
                                CoinA = mutation.Asset.Coin.Symbol;
                                ImageUriA = mutation.Asset.Coin.ImageUri;
                                QtyA = mutation.Qty;
                                PriceA = mutation.Price;
                                ValueA = qtyA * priceA;
                                AccountFrom = mutation.Asset.Account.Name;
                                TransactionDirection = MutationDirection.Out;
                                break;
                            }
                        case TransactionKind.Transfer:
                            {
                                if (mutation.Direction == MutationDirection.In)
                                {
                                    if (transactionType == TransactionKind.Transfer)
                                    {
                                        CoinA = mutation.Asset.Coin.Symbol;
                                        ImageUriA = mutation.Asset.Coin.ImageUri;
                                        QtyA = mutation.Qty;
                                        PriceA = mutation.Price;
                                        ValueA = qtyA * priceA;

                                        AccountTo = mutation.Asset.Account.Name;
                                        if (RequestedAsset != null && RequestedAsset.Account.Name == accountTo)
                                        {
                                            TransactionDirection = MutationDirection.In;
                                        }
                                        else TransactionDirection = MutationDirection.Out;
                                    }
                                }
                                else
                                {
                                    AccountFrom = mutation.Asset.Account.Name;
                                }

                                break;
                            }
                        case TransactionKind.Convert:
                        case TransactionKind.Buy:
                        case TransactionKind.Sell:
                            {

                                if (mutation.Direction == MutationDirection.In)
                                {
                                    CoinB = mutation.Asset.Coin.Symbol;
                                    ImageUriB = mutation.Asset.Coin.ImageUri;
                                    QtyB = mutation.Qty;
                                    PriceB = mutation.Price;
                                    ValueB = qtyB * priceB;
                                    AccountTo = mutation.Asset.Account.Name;

                                    if (RequestedAsset != null && RequestedAsset.Coin.Symbol == CoinB)
                                    {
                                        TransactionDirection = MutationDirection.In;
                                    }
                                    else TransactionDirection = MutationDirection.Out;
                                }
                                else
                                {
                                    CoinA = mutation.Asset.Coin.Symbol;
                                    ImageUriA = mutation.Asset.Coin.ImageUri;
                                    QtyA = mutation.Qty;
                                    PriceA = mutation.Price;
                                    ValueA = qtyA * priceA;
                                    AccountFrom = mutation.Asset.Account.Name;
                                }
                                break;
                            }
                        case TransactionKind.Fee:
                            {
                                FeeCoin = mutation.Asset.Coin.Symbol;
                                ImageUriFee = mutation.Asset.Coin.ImageUri;
                                FeeQty = mutation.Qty;
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

        #endregion Methods

    }

}


