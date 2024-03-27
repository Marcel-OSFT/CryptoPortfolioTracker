using CryptoPortfolioTracker.Enums;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public class Mutation : BaseModel
    {
         
        //***** Constructor
        public Mutation() 
        {
            Asset = new Asset();
            Transaction = new Transaction();
        }

        //******* Public Properties
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

        private TransactionKind type;
        public TransactionKind Type
        {
            get { return type; }
            set
            {
                if (value != type)
                {
                    type = value;
                    OnPropertyChanged(nameof(Type));
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

        private double price;
        public double Price
        {
            get { return price; }
            set
            {
                if (value != price)
                {
                    price = value;
                    OnPropertyChanged(nameof(Price));
                }
            }
        }

        public MutationDirection Direction { get; set; }
        
        //******* Navigation Properties
        public Asset Asset { get; set; }
        public Transaction Transaction { get; set; }



        //******* Public Methods


       
    }
}
