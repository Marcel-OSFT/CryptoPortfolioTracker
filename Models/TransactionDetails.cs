using CommunityToolkit.Mvvm.ComponentModel;
using CryptoPortfolioTracker.Converters;
using CryptoPortfolioTracker.Enums;
using CryptoPortfolioTracker.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public partial class TransactionDetails : BaseModel
    {
        public TransactionDetails()
        {
            
        }

        /// <summary>
        /// Below properties are the values taken from the TranactionDialog and can be used when editing a transaction
        /// </summary>
        public TransactionKind TransactionType { get; set; }
        public string CoinA { get; set; }
        public string CoinB { get; set; }
        public string AccountFrom { get; set; }
        public string AccountTo { get; set; }
        public double QtyA { get; set; }
        public double QtyB { get; set; }
        public double PriceA { get; set; }
        public double PriceB { get; set; }
        public string FeeCoin { get; set; }
        public double FeeQty { get; set; }
     
        /// <summary>
        /// Only used to determine the plus or minus ICON and is set when preparing the view
        /// Also the images and values are used for displaying in a ListView, not for the TransactionDialog
        /// </summary>
        public MutationDirection TransactionDirection { get; set; } = MutationDirection.NotSet;
        public string ImageUriA { get; set; } = string.Empty;
        public string ImageUriB { get; set; } = string.Empty;
        public string ImageUriFee { get; set; } = string.Empty;
        public double ValueA { get; set; } = 0;
        public double ValueB { get; set; } = 0;





    }
}
