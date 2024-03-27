using CryptoPortfolioTracker.Enums;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;


namespace CryptoPortfolioTracker.Models
{
    public class Transaction: BaseModel
    {   
        public Transaction()
        {
           Mutations = new Collection<Mutation>();
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

        private DateTime timeStamp;
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

        private string note;
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


        //******* Navigation Properties
        public ICollection<Mutation> Mutations { get; set; }

        
    }
}
