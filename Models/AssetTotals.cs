
using Microsoft.EntityFrameworkCore;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public class AssetTotals : BaseModel
    {
        private double qty;
        private double costBase;
        private double marketValue;
        private double averageCostPrice;
        private Coin coin;
        private double profitLossPerc;
        private double profitLoss;


        public double Qty
        {
            get { return qty; }
            set
            {
                if (qty == value) return;
                qty = value;
                if (coin != null) MarketValue = qty * coin.Price;
                AverageCostPrice = costBase / qty;
                OnPropertyChanged(nameof(Qty));
            }
        }
        
        public double CostBase
        {
            get { return costBase; }
            set
            {
                if (costBase == value) return;
                costBase = value;
                AverageCostPrice = costBase / qty;
                ProfitLoss = (marketValue - costBase);
                ProfitLossPerc = 100 * ((marketValue - costBase) / costBase);
                OnPropertyChanged(nameof(CostBase));
            }
        }
        
        public double ProfitLossPerc
        {
            get { return profitLossPerc; }
            set
            {
                if (profitLossPerc == value) return;
                profitLossPerc = value;
                OnPropertyChanged(nameof(ProfitLossPerc));
            }
        }
        public double ProfitLoss
        {
            get { return profitLoss; }
            set
            {
                if (profitLoss == value) return;
                profitLoss = value;
                OnPropertyChanged(nameof(ProfitLoss));
            }
        }
        public double MarketValue
        {
            get { return marketValue; }
            set
            {
                if (marketValue == value) return;
                marketValue = value;
                ProfitLoss = (marketValue - costBase);
                ProfitLossPerc = 100 * ((marketValue - costBase) / costBase);
                OnPropertyChanged(nameof(MarketValue));
            }
        }
        
        public double AverageCostPrice
        {
            get { return averageCostPrice; }
            set
            {
                if (averageCostPrice == value) return;
                averageCostPrice = value;               
                OnPropertyChanged(nameof(AverageCostPrice));
            }
        }

        public Coin Coin
        {
            get { return coin; }
            set
            {
                if (coin == value) return;
                coin = value;
                MarketValue = qty * coin.Price;
                OnPropertyChanged(nameof(Coin));
            }
        }
    }
}
