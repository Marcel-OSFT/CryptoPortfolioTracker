//using ABI.Windows.UI;
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
    public class Asset : BaseModel
    {
        //***** Constructor
        public Asset()
        {
            Coin = new Coin();
            Account = new Account();
        }

        private int id;
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

        private double qty;
        public double Qty
        {
            get { return qty; }
            set
            {
                if (value != qty)
                {
                    qty = value;
                    OnPropertyChanged(nameof(Qty));
                }
            }
        }

        private double averageCostPrice;
        public double AverageCostPrice
        {
            get { return averageCostPrice; }
            set
            {
                if (value != averageCostPrice)
                {
                    averageCostPrice = value;
                    OnPropertyChanged(nameof(AverageCostPrice));
                }
            }
        }

        //******* Navigation Properties
        public Coin Coin { get; set; }
        public Account Account { get; set; }
        public ICollection<Mutation> Mutations { get; set;}


    }
}
